using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

        private void btnPathSub_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "STR Files (*.srt)|*.srt";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                ReadAndConvertFile(filePath);
            }
        }

        private void ReadAndConvertFile(string filePath)
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
            gridSub.Refresh();
            gridSub.Update();
        }

        private void gridSub_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            
        }
    }
}
