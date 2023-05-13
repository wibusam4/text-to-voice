using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ATextToVoice
{
    public partial class MainFrm : Form
    {

        public MainFrm()
        {
            InitializeComponent();
        }

        private void MainFrm_Load(object sender, EventArgs e)
        {
            cbxVoice.SelectedIndex = 0;
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

        }

        private void btnGetVoice_Click(object sender, EventArgs e)
        {

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
    }
}
