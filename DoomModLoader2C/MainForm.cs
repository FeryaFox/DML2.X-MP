﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using DoomModLoader2.Entity;
using Microsoft.VisualBasic;
namespace DoomModLoader2
{
    //TODO
    //add about form
    public partial class MainForm : Form
    {
        private string fold_APPDATA;
        private string fold_P36SOFTWARE;
        private string fold_DMLv2;
        private string cfgPreference;
        private string cfgIWAD;
        private string cfgPWAD;
        private string cfgPORT;
        private string cfgPORT_CONFIG;
        private string foldPRESET;

        #region FORM 
        public MainForm()
        {
            InitializeComponent();
            InitializeConfiguration();
            LoadConfiguration();
            CaricaCFG();
            txtMap_TextChanged(null, null);
            chkCustomConfiguration_CheckedChanged(null, null);
            cmbSkill.SelectedIndex = 3;
            if (SharedVar.CHECK_FOR_UPDATE)
            {
                try
                {
                    CheckUpdate(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not get the latest version info..." + Environment.NewLine +
                                    "Please check your internet connection..." + Environment.NewLine +
                                    "ERROR: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }
        }

        private void cmdPlay_Click(object sender, EventArgs e)
        {
            bool err = false;
            if (cmbSourcePort.SelectedItem == null)
            {
                err = true;
                MessageBox.Show("MISSING SOURCE PORT!" + Environment.NewLine + "Please add one...", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (cmbIWAD.SelectedItem == null)
            {
                err = true;
                MessageBox.Show("MISSING IWAD!" + Environment.NewLine + "Please add one...", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (!err)
            {
                var items = lstPWAD.SelectedItems;
                string param = GetParameters();

                //If the user select less than 2 mods it's usless display the mod order dialog
                if (items != null && items.Count > 1)
                {
                    List<PathName> pwads = new List<PathName>();
                    FormMod formMod = new FormMod();
                    formMod.parameters = param;
                    foreach (PathName p in items)
                        pwads.Add(p);
                    formMod.pwads = pwads;
                    formMod.sourcePort = (PathName)cmbSourcePort.SelectedItem;
                    formMod.ShowDialog();
                }
                else
                {
                    if (items.Count == 1)
                        param += " -file \"" + items.Cast<PathName>().FirstOrDefault().path + "\"";

                    StartGame(param);
                }
            }

        }

        private void cmdSavePreset_Click(object sender, EventArgs e)
        {
            try
            {
                if (lstPWAD.SelectedItems != null && lstPWAD.SelectedItems.Count > 0)
                {
                    string name = Interaction.InputBox("Enter a preset name");
                    if (name.Length > 0)
                    {
                        name = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                        string path = Path.Combine(foldPRESET, name + ".dml");
                        FileStream f = File.Create(path);
                        f.Dispose();
                        foreach (PathName p in lstPWAD.SelectedItems)
                        {
                            File.AppendAllText(p.path, path + Environment.NewLine);
                            UpdateConfig(p.path, path);
                        }
                        //LoadConfiguration(); 
                        CaricaPreset();
                        cmbPreset.SelectedItem = cmbPreset.SelectedItem = cmbPreset.Items.Cast<PathName>().Where(P => P.name.Equals(name.ToUpper())).FirstOrDefault();
                    }
                }
                else
                {
                    MessageBox.Show("No mods selected!" + Environment.NewLine + "Please select at least 1 mod.", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while tryng to save your mod preset..." + Environment.NewLine +
                                "ERROR: \"" + ex.Message + "\"" + Environment.NewLine +
                                "Please try again", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chkNoMonster_CheckedChanged(object sender, EventArgs e)
        {
            chkFast.Enabled = !chkNoMonster.Checked;
            chkRespawn.Enabled = !chkNoMonster.Checked;
        }

        private void txtMap_TextChanged(object sender, EventArgs e)
        {
            bool isEnable = !txtMap.Text.Equals(string.Empty);

            chkFast.Enabled = isEnable;
            chkRespawn.Enabled = isEnable;
            cmbSkill.Enabled = isEnable;
            chkNoMonster.Enabled = isEnable;
        }

        private void cmdAddIWAD_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {

                openFileDialog.Filter = "Where's All the Data? (*.wad)|*.wad|" +
                                        "ZIP archive (*.pk3)|*.pk3|" +
                                        "ZIP archive (*.zip)|*.zip|" +
                                        "ZIP archive (*.pak)|*.pak|" +
                                        "7z archive (*.pk7)|*.pk7|" +
                                        "7z archive (*.7z)| *.7z|" +
                                        "Build Engine file (*.grp)|*.grp|" +
                                        "Blood file (*.rff)|*.rff";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {

                    string[] path = openFileDialog.FileNames;
                    foreach (string p in path)
                    {
                        DialogResult resp;
                        if (!CheckIWAD(p))
                        {
                            resp = MessageBox.Show("\"" + Path.GetFileName(p) + "\" does not look like an IWAD..." + Environment.NewLine +
                                             "This means that it's indeed a mod (so should be loaded as \"PWAD\"), or it does not follow the iwad standard (First four bytes conveted to ASCII = \"iwad\")," + Environment.NewLine +
                                             "do you still want to load it as an IWAD?", "Load IWAD?",  MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        }
                        else
                        {
                            resp = DialogResult.Yes;
                        }

                        if (resp == DialogResult.Yes)
                        {
                            UpdateConfig(p, cfgIWAD);
                            CaricaIWAD();
                            cmbIWAD.SelectedItem = cmbIWAD.Items.Cast<PathName>().LastOrDefault();
                        }
                    }


                }
            }
        }

        private void cmdAddSourcePort_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "*.exe|*.exe";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string[] path = openFileDialog.FileNames;
                    foreach (string p in path)
                    {
                        UpdateConfig(p, cfgPORT);
                        CaricaPort();
                        cmbSourcePort.SelectedItem = cmbSourcePort.Items.Cast<PathName>().LastOrDefault();
                    }
                }
            }
        }

        private void cmdAddConfiguration_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = ".ini|*.ini";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string[] path = openFileDialog.FileNames;
                    foreach (string p in path)
                    {
                        UpdateConfig(p, cfgPORT_CONFIG);
                        CaricaPortConfig();
                        cmbPortConfig.SelectedItem = cmbPortConfig.Items.Cast<PathName>().LastOrDefault();
                    }
                }
            }
        }

        private void chkCustomConfiguration_CheckedChanged(object sender, EventArgs e)
        {
            bool siEnabled = chkCustomConfiguration.Checked;
            cmdAddConfiguration.Enabled = siEnabled;
            cmdRemoveConfiguration.Enabled = siEnabled;
            cmbPortConfig.Enabled = siEnabled;
        }

        private void cmdRemoveIWAD_Click(object sender, EventArgs e)
        {
            PathName wad = (PathName)cmbIWAD.SelectedItem;
            RemoveConfig(wad, cfgIWAD);
            cmbIWAD.Text = "";
            CaricaIWAD();
        }

        private void cmdRemoveSourcePort_Click(object sender, EventArgs e)
        {
            PathName PN = (PathName)cmbSourcePort.SelectedItem;
            RemoveConfig(PN, cfgPORT);
            cmbSourcePort.Text = "";
            CaricaPort();
        }

        private void cmdRemoveConfiguration_Click(object sender, EventArgs e)
        {
            PathName PN = (PathName)cmbPortConfig.SelectedItem;
            RemoveConfig(PN, cfgPORT_CONFIG);
            cmbPortConfig.Text = "";
            CaricaPortConfig();
        }

        private void cmdAddPWAD_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {

                openFileDialog.Filter = "Where's All the Data? (*.wad)|*.wad|" +
                                        "ZIP archive (*.pk3)|*.pk3|" +
                                        "ZIP archive (*.zip)|*.zip|" +
                                        "ZIP archive (*.pak)|*.pak|" +
                                        "7z archive (*.pk7)|*.pk7|" +
                                        "7z archive (*.7z)| *.7z|" +
                                        "Build Engine file (*.grp)|*.grp|" +
                                        "Blood file (*.rff)|*.rff";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {

                    string[] path = openFileDialog.FileNames;
                    foreach (string p in path)
                    {
                        UpdateConfig(p, cfgPWAD);
                        CaricaPreset();
                    }
                }
            }
        }

        private void cmdRemovePWAD_Click(object sender, EventArgs e)
        {
            var items = lstPWAD.SelectedItems;
            foreach (PathName p in items)
            {
                RemoveConfig(p, cfgPWAD);
                CaricaPWAD();
            }

        }

        private void cmbPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            try { 
            PathName preset = (PathName)cmbPreset.SelectedItem;

            string[] path = File.ReadAllLines(preset.path);

            for (int i = 0; i < lstPWAD.Items.Count; i++)
            {
                lstPWAD.SetSelected(i, false);
            }



            foreach (string s in path)
            {
                foreach (PathName p in lstPWAD.Items)
                {
                    if (p.path.Contains(s))
                    {
                        int i = lstPWAD.Items.IndexOf(p);
                        lstPWAD.SetSelected(i, true);
                        break;
                    }
                }
            }

            } catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while trying to load your preset..." + Environment.NewLine +                
                               "ERROR: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void checkForUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CheckUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not get the latest version info..." + Environment.NewLine +
                                "Please check your internet connection..." + Environment.NewLine +
                                "ERROR: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Carica menu "about"
        }

        #endregion

        #region METODI

        private void LoadConfiguration()
        {
            CaricaIWAD();
            CaricaPWAD();
            CaricaPort();
            CaricaPortConfig();
            CaricaPreset();
        }

        private void CaricaPreset()
        {
            string[] pathPreset = Directory.GetFiles(foldPRESET);
            List<PathName> presets = new List<PathName>();

            presets = presets.Where(p => p.name != "NONE").ToList();
            foreach (string p in pathPreset)
            {
                PathName preset = new PathName();
                preset.path = p;
                preset.name = Path.GetFileNameWithoutExtension(p).ToUpper();
                presets.Add(preset);
            }
            cmbPreset.DataSource = presets;
            cmbPreset.SelectedItem = cmbPreset.Items.Cast<PathName>().Where(P => P.name.Equals("NONE")).FirstOrDefault();
            //lstPWAD.SetSelected(0, false);

        }

        private void CaricaPortConfig()
        {
            string[] pathPORT_config = File.ReadAllLines(cfgPORT_CONFIG);
            List<PathName> configs = new List<PathName>();
            foreach (string p in pathPORT_config)
            {
                PathName config = new PathName();
                config.path = p;
                config.name = Path.GetFileNameWithoutExtension(p).ToUpper();
                configs.Add(config);
            }
            cmbPortConfig.DataSource = configs;
        }

        private void CaricaPort()
        {
            string[] pathPORT = File.ReadAllLines(cfgPORT);
            List<PathName> ports = new List<PathName>();
            foreach (string p in pathPORT)
            {
                PathName port = new PathName();
                port.path = p;
                port.name = Path.GetFileNameWithoutExtension(p).ToUpper();
                ports.Add(port);
            }
            cmbSourcePort.DataSource = ports;
        }

        private void CaricaPWAD()
        {
            string[] pathPWAD = File.ReadAllLines(cfgPWAD);
            List<PathName> wads = new List<PathName>();
            foreach (string p in pathPWAD)
            {
                PathName wad = new PathName();
                wad.path = p;
                wad.name = Path.GetFileName(p).ToUpper();
                wads.Add(wad);
            }
            lstPWAD.DataSource = wads;
        }

        private void CaricaIWAD()
        {
            string[] pathIWAD = File.ReadAllLines(cfgIWAD);
            List<PathName> wads = new List<PathName>();
            foreach (string p in pathIWAD)
            {
                    PathName wad = new PathName();
                    wad.path = p;
                    wad.name = Path.GetFileNameWithoutExtension(p).ToUpper();
                    wads.Add(wad);
            }

            cmbIWAD.DataSource = wads;
        }

        private void CaricaCFG()
        {
            try
            {
                string[] cfg = File.ReadAllLines(cfgPreference);

                if (cfg.Length > 0)
                {
                    if (!cfg[0].Equals("0"))
                    {
                        if (cfg[0].Equals("1"))
                        {
                            radAudioNoMusic.Checked = true;
                        }
                        else
                        if (cfg[0].Equals("2"))
                        {
                            radAudioNoSFX.Checked = true;
                        }
                        else
                        if (cfg[0].Equals("3"))
                        {
                            radAudioNoSounds.Checked = true;
                        }
                    }



                    if (!cfg[1].Equals("0"))
                    {
                        txtScreenWidth.Text = cfg[1];
                        txtScreenHeight.Text = cfg[2];

                    }

                    chkFullscreen.Checked = cfg[3].Equals("1") ? true : false;



                    if (cfg[4].Equals("1"))
                    {
                        chkCustomConfiguration.Checked = true;
                        cmbPortConfig.SelectedItem = cmbPortConfig.Items.Cast<PathName>().Where(p => p.path == cfg[5]).FirstOrDefault();
                    }

                    txtCommandLine.Text = cfg[6];
                    cmbIWAD.SelectedItem = cmbIWAD.Items.Cast<PathName>().Where(p => p.path == cfg[7]).FirstOrDefault();
                    cmbSourcePort.SelectedItem = cmbSourcePort.Items.Cast<PathName>().Where(p => p.path == cfg[8]).FirstOrDefault();

                    if (!cfg[9].Equals("-1"))
                    {
                        cmb_vidrender.SelectedIndex = int.Parse(cfg[9]);
                    }

                    if (cfg[10].Equals("0"))
                    {
                        SharedVar.CHECK_FOR_UPDATE = false;
                    }
                    else
                    {
                        SharedVar.CHECK_FOR_UPDATE = true;
                    }


                }
                else
                {
                    cmb_vidrender.SelectedIndex = 0;
                    SharedVar.CHECK_FOR_UPDATE = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while trying to load your preferences..." + Environment.NewLine + "Error: \"" + ex.Message + "\"", "FATAL ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmb_vidrender.SelectedIndex = 0;
                SharedVar.CHECK_FOR_UPDATE = true;
            }
        }

        private void InitializeConfiguration()
        {
            try
            {
                fold_APPDATA = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                fold_P36SOFTWARE = Path.Combine(fold_APPDATA, @"P36_Software");
                fold_DMLv2 = Path.Combine(fold_P36SOFTWARE, @"DMLv2");
                foldPRESET = Path.Combine(fold_DMLv2, @"Presets");
                cfgPreference = Path.Combine(fold_DMLv2, @"DMLv2.cfg");
                cfgIWAD = Path.Combine(fold_DMLv2, @"IWAD.cfg");
                cfgPWAD = Path.Combine(fold_DMLv2, @"PWAD.cfg");
                cfgPORT = Path.Combine(fold_DMLv2, @"PORT.cfg");
                cfgPORT_CONFIG = Path.Combine(fold_DMLv2, @"PORT_CONFIG_PATH.cfg");

                if (!Directory.Exists(fold_P36SOFTWARE))
                    Directory.CreateDirectory(fold_P36SOFTWARE);

                if (!Directory.Exists(fold_DMLv2))
                    Directory.CreateDirectory(fold_DMLv2);

                if (!Directory.Exists(foldPRESET))
                    Directory.CreateDirectory(foldPRESET);

                string placeholder = Path.Combine(foldPRESET, "NONE.dml");
                if (!File.Exists(placeholder))
                {
                    FileStream F = File.Create(placeholder);
                    F.Dispose();
                }

                if (!File.Exists(cfgIWAD))
                {
                    FileStream F = File.Create(cfgIWAD);
                    F.Dispose();
                }
                if (!File.Exists(cfgPWAD))
                {
                    FileStream F = File.Create(cfgPWAD);
                    F.Dispose();
                }


                if (!File.Exists(cfgPORT))
                {
                    FileStream F = File.Create(cfgPORT);
                    F.Dispose();
                }


                if (!File.Exists(cfgPreference))
                {
                    FileStream F = File.Create(cfgPreference);
                    F.Dispose();
                }


                if (!File.Exists(cfgPORT_CONFIG))
                {
                    FileStream F = File.Create(cfgPORT_CONFIG);
                    F.Dispose();
                }


            }
            catch (Exception ex)
            {
                StringBuilder errore = new StringBuilder();
                errore.AppendLine("Could not create a .cfg file or folder!");
                errore.AppendLine("Please check if your account have the permission to write in:");
                errore.AppendLine(@"""" + fold_APPDATA + @"""");
                errore.AppendLine();
                errore.AppendLine("Error Message:");
                errore.AppendLine(ex.Message);

                MessageBox.Show(errore.ToString(), "FATAL ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

        }

        private bool CheckIWAD(string path = "")
        {
            try
            {
                if (path.Equals(""))
                {
                    PathName wad = (PathName)cmbIWAD.SelectedItem;
                    path = wad.path;

                }

                //Chex3 seems marked as PWAD while actually is a stand-alone game... so I just skip the bytes check
                if (Path.GetFileName(path).ToUpper().Equals("CHEX3.WAD"))
                {
                    return true;
                }
                else
                {
                    byte[] wadData = File.ReadAllBytes(path).Take(4).ToArray();
                    string s = Encoding.ASCII.GetString(wadData);
                    return s.Equals("IWAD") ? true : false;
                }
            }
            catch
            {
                return false;
            }
        }

        private string GetParameters()
        {
            StringBuilder parm = new StringBuilder();

            //IWAD
            PathName IWAD = (PathName)cmbIWAD.SelectedItem;
            parm.AppendFormat(@" -IWAD ""{0}"" ", IWAD.path);


            //VIDEO
            //RESOLUTION  (Seems broken in gzdoom)
            if (txtScreenHeight.Text != string.Empty && txtScreenWidth.Text != string.Empty)
            {
                parm.AppendFormat(" -width {0} ", txtScreenWidth.Text);
                parm.AppendFormat(" -height {0} ", txtScreenHeight.Text);
            }

            //FULLSCREEN?
            parm.AppendFormat(" +fullscreen {0} ", chkFullscreen.Checked);

            //AUDIO
            if (!radAudioAllSounds.Checked)
            {
                if (radAudioNoMusic.Checked)
                {
                    parm.Append(" -nomusic ");
                }
                else
                if (radAudioNoSFX.Checked)
                {
                    parm.Append(" -nosfx ");
                }
                else
                if (radAudioNoSounds.Checked)
                {
                    parm.Append(" -nosound ");
                }
            }



            //Level 
            if (txtMap.Text != string.Empty)
            {
                //Map
                parm.AppendFormat(@" +map ""{0}"" ", txtMap.Text);

                //Skill
                parm.AppendFormat(" -skill {0} ", cmbSkill.SelectedIndex + 1);

                if (chkNoMonster.Checked)
                {
                    //No Monster
                    parm.Append(" -nomonsters ");
                }
                else
                {
                    //Fast Monster (like Nightmare)
                    if (chkFast.Checked)
                        parm.Append(" -fast ");

                    //Monster respawn (like Nightmare)
                    if (chkRespawn.Checked)
                        parm.Append(" -respawn ");
                }
            }

            if (chkCustomConfiguration.Checked)
            {
                PathName p = (PathName)cmbPortConfig.SelectedItem;
                parm.AppendFormat(@" -config ""{0}""", p.path);
            }

            parm.AppendFormat(" +vid_rendermode {0} ", cmb_vidrender.SelectedIndex);

            parm.Append(" " + txtCommandLine.Text + " ");
            return parm.ToString();

        }

        public void StartGame(string param)
        {
            try
            {
                PathName sp = (PathName)cmbSourcePort.SelectedItem;
                Process.Start(sp.path, param);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot start the game!" + Environment.NewLine +
                              "ERROR: \"" + ex.Message + "\"", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateConfig(string ItemPath, string cfgPath)
        {

            try
            {
                string s = File.ReadAllLines(cfgPath).Where(P => P == ItemPath).FirstOrDefault();
                if (s == null)
                {
                    File.AppendAllText(cfgPath, ItemPath + Environment.NewLine);
                    //LoadConfiguration();
                }
                else
                {
                    MessageBox.Show("Cannot add the same file multiple time!" + Environment.NewLine +
                                    "The following path has already been added:" + Environment.NewLine +
                                    "\"" + ItemPath + "\"", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdateRemoveConfigError(ex, cfgPath);
            }


        }

        private void RemoveConfig(PathName PN, string cfgPath)
        {
            try
            {
                if (PN != null)
                {
                    DialogResult ris = MessageBox.Show("Are you sure you want to remove \"" + PN.name + "\""
                                       + Environment.NewLine
                                       + "(Path: \"" + PN.path + "\")"
                                       , "REMOVE " + PN.name.ToUpper(), MessageBoxButtons.OKCancel);

                    if (ris == DialogResult.OK)
                    {
                        string[] s = File.ReadAllLines(cfgPath).Where(P => P != PN.path).ToArray();

                        File.WriteAllLines(cfgPath, s);
                        //LoadConfiguration();
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateRemoveConfigError(ex, cfgPath);
            }
        }

        private void UpdateRemoveConfigError(Exception ex, string cfgPath)
        {
            StringBuilder errore = new StringBuilder();
            errore.AppendLine("Something went wrong while trying to update a configuration file...");
            errore.AppendLine("Please check if your account have the permission to write in:");
            errore.AppendLine(@"""" + cfgPath + @"""");
            errore.AppendLine();
            errore.AppendLine("Error Message:");
            errore.AppendLine(ex.Message);

            MessageBox.Show(errore.ToString(), "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void cmdRemovePreset_Click(object sender, EventArgs e)
        {
            try
            {
                PathName pn = (PathName)cmbPreset.SelectedItem;
                if (pn != null && !pn.name.Equals("NONE"))
                {
                    DialogResult ris = MessageBox.Show("Are you sure you want to remove \"" + pn.name + "\""
                                       + Environment.NewLine
                                       + "(Path: \"" + pn.path + "\")"
                                       , "REMOVE " + pn.name.ToUpper(), MessageBoxButtons.OKCancel);

                    if (ris == DialogResult.OK)
                    {
                        File.Delete(pn.path);
                        CaricaPreset();
                    }

                }
            }
            catch (Exception Ex)
            {
                UpdateRemoveConfigError(Ex, foldPRESET);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            string[] EXIT_MESSAGES = new string[] {
                //DOOM
                "Please don't leave, there's more demons to toast!",
                "Let's beat it -- This is turning into a bloodbath!",
                "I wouldn't leave if I were you. DOS is much worse.",
                "You're trying to say you like DOS better than me, right?",
                "Don't leave yet -- There's a demon around that corner!",
                "Ya know, next time you come in here I'm gonna toast ya.",
                "Go ahead and leave. See if I care.",
                "Are you sure you want to quit this great game? ",
            };

            Random R = new Random();

            DialogResult ris = MessageBox.Show(EXIT_MESSAGES[R.Next(0, EXIT_MESSAGES.Length)], "QUIT?", MessageBoxButtons.YesNo);

            if (ris == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                SavePreferences();
            }
        }

        private void SavePreferences()
        {
            try
            {
                StringBuilder preferences = new StringBuilder();
                //Audio 1 0
                if (radAudioAllSounds.Checked)
                {
                    preferences.AppendLine("0");
                }
                else
                    if (radAudioNoMusic.Checked)
                {
                    preferences.AppendLine("1");
                }
                else
                    if (radAudioNoSFX.Checked)
                {
                    preferences.AppendLine("2");
                }
                else
                    if (radAudioNoSounds.Checked)
                {
                    preferences.AppendLine("3");
                }

                //Video 2 2
                if (txtScreenHeight.Text != string.Empty && txtScreenWidth.Text != string.Empty)
                {
                    preferences.AppendLine(txtScreenWidth.Text);
                    preferences.AppendLine(txtScreenHeight.Text);
                }
                else
                {
                    preferences.AppendLine("0");
                    preferences.AppendLine("");
                }

                //fullscreen 1 3
                if (chkFullscreen.Checked)
                {
                    preferences.AppendLine("1");
                }
                else
                {
                    preferences.AppendLine("0");
                }

                //Config 2 5
                if (chkCustomConfiguration.Checked)
                {
                    preferences.AppendLine("1");
                    PathName p = (PathName)cmbPortConfig.SelectedItem;
                    if (p != null)
                    {
                        preferences.AppendLine(p.path);
                    }
                }
                else
                {
                    preferences.AppendLine("0");
                    preferences.AppendLine("");
                }

                //txtCommand 1 6
                preferences.AppendLine(txtCommandLine.Text);

                //iwad 1 7
                PathName iwad = (PathName)cmbIWAD.SelectedItem;

                //pwad 1 8
                PathName port = (PathName)cmbSourcePort.SelectedItem;

                if (iwad != null)
                {
                    preferences.AppendLine(iwad.path);
                }
                else
                {
                    preferences.AppendLine("NULL");
                }


                if (port != null)
                {
                    preferences.AppendLine(port.path);
                }
                else
                {
                    preferences.AppendLine("NULL");
                }


                preferences.AppendLine(cmb_vidrender.SelectedIndex.ToString());

                if (SharedVar.CHECK_FOR_UPDATE)
                {
                    preferences.AppendLine("1");
                }
                else
                {
                    preferences.AppendLine("0");
                }

                File.WriteAllText(cfgPreference, preferences.ToString());
            }
            catch (Exception ex)
            {
                UpdateRemoveConfigError(ex, cfgPreference);
            }
        }

        private void CheckUpdate(bool start = false)
        {
            try
            {
                VersionForm vf = new VersionForm();
                if (start)
                {
                    if (!vf.isLatestVersion())
                    {
                        vf.ShowDialog();
                    }
                }
                else
                {
                    vf.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not get the latest version info..." + Environment.NewLine +
                                "Please check your internet connection..." + Environment.NewLine +
                                "ERROR: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }


        #endregion
    }
}