using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ATextToVoice
{
    public partial class MainFrm : Form
    {

        public MainFrm()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        private void MainFrm_Load(object sender, EventArgs e)
        {
            cbxVoice.SelectedIndex = 0;
            loadKey();
        }

        private void btnPathSub_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "STR Files (*.srt)|*.srt";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                readAndConvertFile(filePath);
            }
        }

        private void gridSub_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            
        }
        
        private void btnAudio_Click(object sender, EventArgs e)
        {
            testVoice();
        }

        private void btnSaveKey_Click(object sender, EventArgs e)
        {
            try
            {
                saveKey(txtApiKey.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private async void btnGetVoice_Click(object sender, EventArgs e)
        {
            var progress = new Progress<int>(percent =>
            {
                prbProcess.Value = percent;
            });
            await convertVoiceSubsToAudio(progress);
           
        }

        private void gridSub_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 5)
            {
                try
                {
                    VoiceSub voiceSub = voiceSubBindingSource.Current as VoiceSub;

                    mediaPlayer.URL = $"{Environment.CurrentDirectory}\\mp3\\{voiceSub.id}.mp3";
                    mediaPlayer.Ctlcontrols.play();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void testVoice()
        {
            try
            {
                string id = cbxVoice.SelectedItem.ToString().Split('.')[0];
                mediaPlayer.URL = string.Format("{0}\\voice\\{1}.mp3", Environment.CurrentDirectory, id);
                mediaPlayer.Ctlcontrols.play();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void readAndConvertFile(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        int id;
                        if (int.TryParse(line, out id))
                        {
                            line = reader.ReadLine(); // Đọc dòng kế tiếp chứa thời gian
                            string[] timeData = line.Split(new string[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);

                            if (timeData.Length == 2)
                            {
                                TimeSpan start;
                                if (TimeSpan.TryParseExact(timeData[0].Trim(), "hh\\:mm\\:ss\\,fff", CultureInfo.InvariantCulture, out start))
                                {
                                    TimeSpan end;
                                    if (TimeSpan.TryParseExact(timeData[1].Trim(), "hh\\:mm\\:ss\\,fff", CultureInfo.InvariantCulture, out end))
                                    {
                                        line = reader.ReadLine();
                                        string sub = line.Trim();
                                        line = reader.ReadLine();
                                        string status = "";
                                        VoiceSub voiceSub = new VoiceSub(id, sub, start, end, status);
                                        VoiceSub.listVoiceSub.Add(voiceSub);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            voiceSubBindingSource.DataSource = null;
            voiceSubBindingSource.DataSource = VoiceSub.listVoiceSub;
        }

        private void saveKey(string key)
        {
            File.WriteAllText(Environment.CurrentDirectory + "//key//key.txt", key);
            MessageBox.Show("Lưu thành công", "Success", MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        private void loadKey()
        {
            string filePath = Environment.CurrentDirectory + "\\key\\key.txt";
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
                return;
            }
            string key = File.ReadAllText(Environment.CurrentDirectory + "\\key\\key.txt");
            txtApiKey.Text = key;
        }

        public async Task convertVoiceSubsToAudio(IProgress<int> progress)
        {
            int voiceSubCount = VoiceSub.listVoiceSub.Count;
            foreach (VoiceSub voiceSub in VoiceSub.listVoiceSub)
            {
                
                voiceSub.status = "Loading";
                string apiUrl = "https://api.fpt.ai/hmi/tts/v5";
                using (HttpClient client = new HttpClient())
                {
                    
                    client.DefaultRequestHeaders.Add("api-key", txtApiKey.Text);
                    client.DefaultRequestHeaders.Add("speed", "");
                    client.DefaultRequestHeaders.Add("voice", "banmai");
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(voiceSub.sub));

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        dynamic result = JsonConvert.DeserializeObject(responseContent);

                        string asyncLink = result.async;
                        using (HttpClient audioClient = new HttpClient())
                        {
                            byte[] audioBytes = await audioClient.GetByteArrayAsync(asyncLink);

                            string filePath = $"{Environment.CurrentDirectory}\\mp3\\{voiceSub.id}.mp3"; 

                            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await fileStream.WriteAsync(audioBytes, 0, audioBytes.Length);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Error at {voiceSub.id}");
                    }
                }
                voiceSub.status = "Thành công";
                int percent = (int)((voiceSub.id) / (double)voiceSubCount * 100);
                progress.Report(percent);
                voiceSubBindingSource.DataSource = null;
                voiceSubBindingSource.DataSource = VoiceSub.listVoiceSub;
                await Task.Delay(500);
            }
            voiceSubBindingSource.DataSource = null;
            voiceSubBindingSource.DataSource = VoiceSub.listVoiceSub;
        }
    }
}
