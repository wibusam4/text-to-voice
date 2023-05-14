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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ATextToVoice
{
    public partial class MainFrm : Form
    {
        private string key;
        private string myVoice;
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
            key = txtApiKey.Text;
            myVoice = getVoice(cbxVoice.SelectedIndex);
            var progress = new Progress<int>(percent =>
            {
                prbProcess.Value = percent;
            });
            await convertVoiceSubsToAudio(progress);
            await getVoiceFromLink();
        }

        private void btnGhepVoice_Click(object sender, EventArgs e)
        {
            //cutAudio();
            concatenateAudio();
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
            string filePathMp3 = Environment.CurrentDirectory + "\\mp3\\";
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(filePathMp3))
            {
                Directory.CreateDirectory(filePathMp3);
            }
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

        private void refreshGrid()
        {
            gridSub.Update();
            gridSub.Refresh();
        }

        private async Task getVoiceFromLink()
        {
            List<Task> conversionTasks = new List<Task>();
            foreach (VoiceSub voiceSub in VoiceSub.listVoiceSub)
            {
                Task conversionTask = Task.Run(async () =>
                {
                    if (voiceSub.status.Contains("https"))
                    {
                        using (HttpClient audioClient = new HttpClient())
                        {
                            byte[] audioBytes = await audioClient.GetByteArrayAsync(voiceSub.status);

                            string filePath = $"{Environment.CurrentDirectory}\\mp3\\{voiceSub.id}.mp3";

                            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await fileStream.WriteAsync(audioBytes, 0, audioBytes.Length);
                                voiceSub.status = "Thành công";
                                refreshGrid();
                            }
                        }
                    }
                });
                conversionTasks.Add(conversionTask);
            }
            await Task.WhenAll(conversionTasks);
        }

        public async Task convertVoiceSubsToAudio(IProgress<int> progress)
        {
            List<Task> conversionTasks = new List<Task>();

            foreach (VoiceSub voiceSub in VoiceSub.listVoiceSub)
            {
                int index = 0;
                int count = VoiceSub.listVoiceSub.Count;
                Task conversionTask = Task.Run(async () =>
                {
                    string apiUrl = "https://api.fpt.ai/hmi/tts/v5";

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("api-key", "mzetFpsTMI1055XOP5pCMeqsbEPsJEqW");
                        client.DefaultRequestHeaders.Add("speed", "+1");
                        client.DefaultRequestHeaders.Add("voice", "banmai");
                        HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(voiceSub.sub));
                        
                        if (response.IsSuccessStatusCode)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            dynamic result = JsonConvert.DeserializeObject(responseContent);
                            string asyncLink = result.async;
                            voiceSub.status = asyncLink;
                            refreshGrid();
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            voiceSub.status = responseContent;
                            refreshGrid();
                        }
                        index++;
                        int percent = (int)((index + 1) / (double)count * 100);
                        progress.Report(percent);
                    }
                });
                conversionTasks.Add(conversionTask);
            }
            await Task.WhenAll(conversionTasks);
        }

        private string getVoice(int id)
        {
            switch (id)
            {
                case 0:
                    return "banmai";
                case 1:
                    return "thuminh";
                case 2:
                    return "myan";
                case 3:
                    return "ngoclam";
                case 4:
                    return "linhsan";
                case 5:
                    return "lannhi";
                default:
                    return "banmai";
            }
        }

        private void concatenateAudio()
        {
            string fileListPath = Path.Combine(Path.GetTempPath(), "filelist.txt");

            try
            {

                List<string> fileLines = new List<string>();

                for (int i = 0; i < VoiceSub.listVoiceSub.Count; i++)
                {
                    VoiceSub voiceSub = VoiceSub.listVoiceSub[i];
                    string filePath = $"{Environment.CurrentDirectory}\\mp3\\{voiceSub.id}.mp3";
                    if (File.Exists(filePath))
                    {
                        string start = voiceSub.start.ToString(@"hh\:mm\:ss\.fff");
                        fileLines.Add($"file '{filePath}'");
                    }
                }

                File.WriteAllLines(fileListPath, fileLines);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = $"{Environment.CurrentDirectory}\\ffmpeg\\ffmpeg.exe",
                    Arguments = $"-f concat -safe 0 -i \"{fileListPath}\" -c copy \"{Environment.CurrentDirectory}\\mp3\\x.mp3\"",
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    process.WaitForExit();
                }
            }
            finally
            {
                File.Delete(fileListPath);
            }
        }

        public void cutAudio()
        {

            foreach (VoiceSub voiceSub in VoiceSub.listVoiceSub)
            {
                string inputFilePath = $"{Environment.CurrentDirectory}\\mp3\\{voiceSub.id}.mp3";
                string outputFilePath = $"{Environment.CurrentDirectory}\\mp3 - Copy\\{voiceSub.id}.mp3";
                TimeSpan audioDuration = getAudioDuration(inputFilePath);
                TimeSpan subDuration = voiceSub.end - voiceSub.start;
                double speed = audioDuration.TotalSeconds / subDuration.TotalSeconds;
                if(speed > 1.0)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = $"{Environment.CurrentDirectory}\\ffmpeg\\ffmpeg.exe",
                        Arguments = $"-i \"{inputFilePath}\" -filter:a \"atempo = {speed}\" \"{outputFilePath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    };

                    using (Process process = new Process { StartInfo = startInfo })
                    {
                        process.Start();
                        process.WaitForExit();
                    }
                }
            }
        }

        public TimeSpan getAudioDuration(string filePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = $"{Environment.CurrentDirectory}\\ffmpeg\\ffmpeg.exe",
                Arguments = $"-i \"{filePath}\" -hide_banner",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Regex regex = new Regex(@"Duration: (\d{2}):(\d{2}):(\d{2}\.\d+)");
                Match match = regex.Match(output);

                if (match.Success)
                {
                    int hours = int.Parse(match.Groups[1].Value);
                    int minutes = int.Parse(match.Groups[2].Value);
                    double seconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                    TimeSpan duration = new TimeSpan(hours, minutes, 0) + TimeSpan.FromSeconds(seconds);
                    return duration;
                }
                else
                {
                }
            }

            return TimeSpan.Zero;
        }
    }
}
