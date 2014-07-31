using System;
using System.Windows.Forms;
using Mogre;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace GameX
{
    public partial class Toolbox : Form
    {

        public Toolbox()
        {
            InitializeComponent();
        }

        private void UpdateSystemInfo(bool sys, bool emit)
        {
            if(sys){
                string[] names = App.Singleton.xGetSystemNames();
                systemList.DataSource = names;
                systemList.Update();
            }
            
            if(emit){
                string[] em_names = App.Singleton.xGetEmitterNames();
                emitterList.DataSource = em_names;
                emitterList.Update();
            }

            // update parameters
            ParticleSystem ps = App.Singleton.xGetCurrentSystem();
            if (ps != null)
            {
                sysOnChk.Checked = ps.Emitting;
                cullEachChk.Checked = ps.CullIndividually;
                sortChk.Checked = ps.SortingEnabled;
                pWidthBox.Text = ps.DefaultWidth.ToString();
                pHeightBox.Text = ps.DefaultHeight.ToString();
                matCombo.Text = ps.MaterialName;
                billboardBox.Text = ps.Renderer.GetParameter("billboard_type");
                
                if(billboardBox.Text.IndexOf("common")!=-1){
                    String comm = ps.Renderer.GetParameter("common_direction");
                    switch(comm){
                        case "0,0,1": commonDir.SelectedText = "Z axis"; break;
                        case "0,1,0": commonDir.SelectedText = "Y axis"; break;
                        case "1,0,0": commonDir.SelectedText = "X axis"; break;
                        default: commonDir.SelectedText = "Z axis"; break;
                    }
                }else{
                    commonDir.SelectedIndex = 0;
                }
                quotaBox.Text = ps.ParticleQuota.ToString();

                groupBox.Enabled = true;
                emitTypeList.Enabled = true;
            }else{
                groupBox.Enabled = false;
                emitTypeList.Enabled = false;
            }
        }

        private void UpdateEmitterValues(ParticleEmitter pe)
        {
            if (pe == null)
            {
                tabBox.TabPages.Remove(emitterTab);
                return;
            }

            // add emitter options
            if (tabBox.TabPages.Contains(emitterTab) == false)
            {
                tabBox.TabPages.Add(emitterTab);
            }

            posx.Text = pe.Position.x.ToString();
            posy.Text = pe.Position.y.ToString();
            posz.Text = pe.Position.z.ToString();

            dirx.Text = pe.Direction.x.ToString();
            diry.Text = pe.Direction.y.ToString();
            dirz.Text = pe.Direction.z.ToString();

            color0.BackColor = Color.FromArgb(
                (int)(pe.ColourRangeStart.r*255),
                (int)(pe.ColourRangeStart.g*255),
                (int)(pe.ColourRangeStart.b*255));

            color0a.Value = (decimal)pe.ColourRangeStart.a * 255;

            color1.BackColor = Color.FromArgb(
                (int)(pe.ColourRangeEnd.r * 255),
                (int)(pe.ColourRangeEnd.g * 255),
                (int)(pe.ColourRangeEnd.b * 255));

            color1a.Value = (decimal)pe.ColourRangeEnd.a * 255;

            emissBox.Text = pe.EmissionRate.ToString();
            angleBox.Text = pe.Angle.ValueDegrees.ToString();
            
            velMin.Text = pe.MinParticleVelocity.ToString();
            velMax.Text = pe.MaxParticleVelocity.ToString();

            lifetimeMin.Text = pe.MinTimeToLive.ToString();
            lifetimeMax.Text = pe.MaxTimeToLive.ToString();
            
            durMin.Text = pe.MinDuration.ToString();
            durMax.Text = pe.MaxDuration.ToString();

            delayMin.Text = pe.MinRepeatDelay.ToString();
            delayMax.Text = pe.MaxRepeatDelay.ToString();

            emitBox.Text = pe.EmittedEmitter;

        }

        private void UpdateAffectorValues(bool reset)
        {
            if(reset) 
            {
                // re-fill tables
                ParticleSystem ps = App.Singleton.xGetCurrentSystem();
                if (ps == null)
                {
                    tabBox.TabPages.Remove(affectorTab);
                    return;
                }

                App.Singleton.xSelectAffector(null);

                if (tabBox.TabPages.Contains(affectorTab) == false)
                {
                    tabBox.TabPages.Add(affectorTab);
                }

                List<string> namelist = new List<string>();
                for (int i = 0; i < ps.NumAffectors; i++)
                {
                    ParticleAffector af = ps.GetAffector((ushort)i);
                    namelist.Add("Affector" + i + ":" + af.Type);
                }
                affectorList.DataSource = namelist;
                affectorList.Update();

                if (affectorList.Items.Count > 0)
                {
                    // select first affector
                    affectorList.SelectedIndex = 0;
                    if(affectorBox.Items.Count > 0) affectorBox.SelectedIndex = 0;
                    ParticleAffector af = ps.GetAffector(0);
                    App.Singleton.xSelectAffector(af);
                }
            }
            
            // list selected affector values
            ParticleAffector curr = App.Singleton.xGetCurrentAffector();
            affectorParamGrid.DataSource = App.Singleton.xGetExtraAffectorParams(curr);
            affectorParamGrid.Update();
        }
        

        private void UpdateParamTab(ParticleEmitter pe){

            if(pe == null){
                tabBox.TabPages.Remove(extraTab);
                return;
            }

            Dictionary<string, string> defparam = App.Singleton.xGetDefEmitterParams(pe.Type); 

            if (tabBox.TabPages.Contains(extraTab) == false)
            {
                // emitter has additional parameters
                if(defparam.Count > 0)
                {
                    tabBox.TabPages.Add(extraTab);
                   
                    dataGrid.DataSource = App.Singleton.xGetExtraEmitterParams(pe);
                    dataGrid.Columns[0].ReadOnly = true;
                    dataGrid.Update();
                }
            }
            else if (tabBox.TabPages.Contains(extraTab)) // no parameters for emitter
            {
                // remove pane
                if (defparam.Count == 0)
                {
                    tabBox.TabPages.Remove(extraTab);
                    dataGrid.DataSource = null;
                    dataGrid.Update();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ParticleSystem ps = App.Singleton.xAddParticleSystem();
            if (ps == null) return;
            App.Singleton.xSelectSystem(ps);
            if (ps != null)
            {
                //TODO: sinchronize in different way
                ps.FastForward(App.Singleton.xGetSystemTime());
            }
            systemList.Enabled = true;

            UpdateAffectorValues(true);
            UpdateSystemInfo(true, true);
        }

        private void systemList_SelectedIndexChanged(object sender, EventArgs e)
        {
           if( systemList.SelectedItem != null)
           {
               // select
               delSystemBtn.Enabled = true;
               emitterList.Enabled = true;
               btnAddEmitter.Enabled = true;
               btnUpdate.Enabled = true;
               App.Singleton.xSelectSystem((string)systemList.SelectedItem);
               App.Singleton.xSelectEmitter((ParticleEmitter)null);

               ParticleSystem pS = App.Singleton.xGetCurrentSystem();
               if (pS != null)
               {
                   pWidthBox.Text = pS.DefaultWidth.ToString();
                   pHeightBox.Text = pS.DefaultHeight.ToString();

                   if (tabBox.TabPages.Contains(affectorTab) == false)
                   {
                       tabBox.TabPages.Add(affectorTab);
                   }
               }else{
                   tabBox.TabPages.Remove(affectorTab);
               }
           }else{
               // deselect
               delSystemBtn.Enabled = false;
               emitterList.Enabled = false;
               btnAddEmitter.Enabled = false;
               btnUpdate.Enabled = false;
               App.Singleton.xSelectSystem((ParticleSystem)null);
               App.Singleton.xSelectEmitter((ParticleEmitter)null);
           }
           UpdateAffectorValues(true);
           UpdateSystemInfo(false, true);
        }

        private void btnAddEmitter_Click(object sender, EventArgs e)
        {
            string newType = (string)emitTypeList.SelectedItem;
            Dictionary<string, string> defparam = App.Singleton.xGetDefEmitterParams(newType);

            ParticleEmitter pe = App.Singleton.xAddTypedEmitter(newType, defparam);
            if (pe == null) return;
            App.Singleton.xSelectEmitter(pe);

            emitterList.Enabled = true;
            emitTypeLbl.Text = pe.Type;

            UpdateSystemInfo(false, true);
            UpdateEmitterValues(App.Singleton.xGetCurrentEmitter());
            UpdateParamTab(pe);

            emitterList.SelectedItem = pe.Name;
        }

        private void Toolbox_Load(object sender, EventArgs e)
        {
            tabBox.TabPages.Remove(emitterTab);
            tabBox.TabPages.Remove(extraTab);
            tabBox.TabPages.Remove(affectorTab);
           
            string[] mats = App.Singleton.xGetParticleMaterials();
            matCombo.Items.Clear();
            matCombo.DataSource = mats;
            matCombo.Update();

            string[] types = App.Singleton.xGetEmitterTypes();
            emitTypeList.Items.Clear();
            emitTypeList.DataSource = types;
            emitTypeList.Update();
            if(types.Length > 0) emitTypeList.SelectedItem = types[0];

            string[] billtypes = App.Singleton.xGetBillboardTypes();
            billboardBox.Items.Clear();
            billboardBox.DataSource = billtypes;
            billboardBox.Update();
            if (billtypes.Length > 0) billboardBox.SelectedItem = billtypes[0];

            string[] affectypes = App.Singleton.xGetAffectorTypes();
            affectorBox.Items.Clear();
            affectorBox.DataSource = affectypes;
            affectorBox.Update();
            if (affectypes.Length > 0) affectorBox.SelectedItem = affectypes[0];

            commonDir.SelectedIndex = 0;
        }

        private void Toolbox_FormClosing(object sender, FormClosingEventArgs e)
        {
            App.Singleton.ShutDown();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (emitterList.SelectedItem != null)
            {
                // select new emitter
                delEmitterBtn.Enabled = true;
                emitterList.Enabled = true;
                App.Singleton.xSelectEmitter((string)emitterList.SelectedItem);
                UpdateEmitterValues(App.Singleton.xGetCurrentEmitter());
                ParticleEmitter pe = App.Singleton.xGetCurrentEmitter();

                // successfully selected
                if (pe != null)
                {
                    emitterTab.Text = "Emitter: " + pe.Name;
                    emitTypeLbl.Text = pe.Type;
                }
                else // failed to select
                {
                    // deselect
                    emitterList.Enabled = false;
                    App.Singleton.xSelectEmitter((ParticleEmitter)null);
                }
                UpdateParamTab(pe);
            }else{
                delEmitterBtn.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ParticleSystem pS = App.Singleton.xGetCurrentSystem();
            if (pS != null && matCombo.Text != "")
            {
                pS.SetMaterialName(matCombo.Text);
                try
                {
                    // assign new values
                    pS.ParticleQuota = uint.Parse(quotaBox.Text);
                    pS.SetDefaultDimensions(float.Parse(pWidthBox.Text),
                        float.Parse(pHeightBox.Text));
                    pS.Renderer.SetParameter("billboard_type", billboardBox.Text);
                    
                    // common direction
                    if (billboardBox.Text.IndexOf("common") != -1)
                    {
                        Console.WriteLine(pS.Renderer.GetParameter("common_direction"));
                        Vector3 dir;
                        switch(commonDir.Text){
                            case "Z axis": dir = Vector3.UNIT_Z; break;
                            case "Y axis": dir = Vector3.UNIT_Y; break;
                            case "X axis": dir = Vector3.UNIT_X; break;
                            default: dir = Vector3.UNIT_Z; break;
                        }
                        pS.Renderer.SetParameter("common_direction", StringConverter.ToString(dir));
                    }
                }
                catch (Exception err)
                {
                    MessageBox.Show("Error: " + err);
                }

                UpdateSystemInfo(false, false);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            ParticleEmitter pe = App.Singleton.xGetCurrentEmitter();
            if (pe == null) return;

            const float EPS = 1e-3f;

            // update values based on settings
            try
            {
                Vector3 pos = new Vector3(float.Parse(posx.Text),
                    float.Parse(posy.Text), float.Parse(posz.Text));

                Vector3 dir = new Vector3(float.Parse(dirx.Text),
                    float.Parse(diry.Text), float.Parse(dirz.Text));
                if (dir.Length <= EPS) throw new Exception("Direction length is zero");
                dir.Normalise();


                ColourValue colorStart = new ColourValue(
                    color0.BackColor.R/255.0f,
                    color0.BackColor.G/255.0f,
                    color0.BackColor.B/255.0f,
                    (float)color0a.Value/255.0f);

                ColourValue colorEnd = new ColourValue(
                    color1.BackColor.R / 255.0f,
                    color1.BackColor.G / 255.0f,
                    color1.BackColor.B / 255.0f,
                    (float)color1a.Value / 255.0f);

                uint emission = uint.Parse(emissBox.Text);
                if (emission == 0) throw new Exception("Emission cannot be 0");

                float angle = float.Parse(angleBox.Text);
                if (angle < -EPS) throw new Exception("Angle cannot be negative");

                Vector2 vel = new Vector2(float.Parse(velMin.Text), float.Parse(velMax.Text));
                if (vel.x > vel.y + EPS) throw new Exception("Invalid velocity range");

                Vector2 life = new Vector2(float.Parse(lifetimeMin.Text), float.Parse(lifetimeMax.Text));
                if (life.x > life.y + EPS) throw new Exception("Invalid lifetime range");

                Vector2 dur = new Vector2(float.Parse(durMin.Text), float.Parse(durMax.Text));
                if (dur.x > dur.y + EPS) throw new Exception("Invalid duration range");

                Vector2 delay = new Vector2(float.Parse(delayMin.Text), float.Parse(delayMax.Text));
                if (delay.x > delay.y + EPS) throw new Exception("Invalid delay range");

                pe.SetColour(colorStart, colorEnd);
                pe.SetDuration(dur.x, dur.y);
                pe.SetParticleVelocity(vel.x, vel.y);
                pe.SetRepeatDelay(delay.x, delay.y);
                pe.SetTimeToLive(life.x, life.y);
                pe.Angle = (new Degree(angle)).ValueRadians;
                pe.EmissionRate = emission;
                pe.Direction = dir;
                pe.Position = pos;

                App.Singleton.xUpdateEmitter(pe);
            }
            catch (Exception err)
            {
                MessageBox.Show("Error: " + err);
            }

            UpdateEmitterValues(pe);

        }


        private void sysOnChk_CheckedChanged_1(object sender, EventArgs e)
        {
            ParticleSystem pS = App.Singleton.xGetCurrentSystem();
            if (pS != null)
            {
                pS.Emitting = sysOnChk.Checked;
            }
        }

        private void cullEachChk_CheckedChanged(object sender, EventArgs e)
        {
            ParticleSystem pS = App.Singleton.xGetCurrentSystem();
            if (pS != null)
            {
                pS.CullIndividually = cullEachChk.Checked;
            }
        }

        private void sortChk_CheckedChanged_1(object sender, EventArgs e)
        {
            ParticleSystem pS = App.Singleton.xGetCurrentSystem();
            if (pS != null)
            {
                pS.SortingEnabled = sortChk.Checked;
            }
        }

        private void dataGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                DataGridViewRow row = dataGrid.Rows[e.RowIndex];
                string paramName = (string)row.Cells[0].Value;
                string value = (string)row.Cells[1].Value;
                ParticleEmitter pe = App.Singleton.xGetCurrentEmitter();
                if (pe != null)
                {
                    pe.SetParameter(paramName, value);
                    UpdateParamTab(pe);
                }
            }
            catch(Exception err)
            {
                MessageBox.Show("Error: invalid parameter " + err);
            }
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            App.Singleton.xRotateCamera((e.NewValue-e.OldValue)*3.6f); // to 360 degrees
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            App.Singleton.xToggleSkybox(skyChk.Checked);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            App.Singleton.xToggleEmitterModel(emitShowChk.Checked);
        }

        private void hScrollBar2_Scroll(object sender, ScrollEventArgs e)
        {
            App.Singleton.xZoomCamera(e.NewValue);
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            colorDlg.ShowDialog();
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            colorDlg.Color = color0.BackColor;
            if(colorDlg.ShowDialog() == DialogResult.OK)
            {
                color0.BackColor = colorDlg.Color;
            }
        }

        private void color1_MouseClick(object sender, MouseEventArgs e)
        {
            colorDlg.Color = color1.BackColor;
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                color1.BackColor = colorDlg.Color;
            }
        }

        private void billboardBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string msg = (string)billboardBox.SelectedItem;
            bool state = (msg.IndexOf("common") != -1);
            commonDir.Enabled = state;
        }

        private void listBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            ParticleSystem ps = App.Singleton.xGetCurrentSystem();
            if (ps == null) return;

            ParticleAffector af = ps.GetAffector((ushort)affectorList.SelectedIndex);
            App.Singleton.xSelectAffector(af);
            UpdateAffectorValues(false);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ParticleAffector af = App.Singleton.xAddAffector(affectorBox.Text);
            if (af == null) return;
            App.Singleton.xSelectAffector(af);
            UpdateAffectorValues(true);
        }

        private void affectorParamGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                DataGridViewRow row = affectorParamGrid.Rows[e.RowIndex];
                string paramName = (string)row.Cells[0].Value;
                string value = (string)row.Cells[1].Value;
                ParticleAffector af = App.Singleton.xGetCurrentAffector();
                if (af != null)
                {
                    af.SetParameter(paramName, value);
                    UpdateAffectorValues(false);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Error: invalid parameter " + err);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ParticleSystem ps = App.Singleton.xGetCurrentSystem();
            if (ps == null) return;
            if (affectorList.SelectedIndex >= 0){
                ps.RemoveAffector((ushort)affectorList.SelectedIndex);
                UpdateAffectorValues(true);
            }
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            ParticleSystem ps = App.Singleton.xGetCurrentSystem();
            if (ps == null) return;

            if (systemList.SelectedIndex >= 0) // has selected a system
            {
                // remove it
                App.Singleton.xRemoveSystem(ps); // delete model data
  
                if (systemList.Items.Count-1 > 0)
                {
                    // select the first one
                    int index = -1;
                    for (int i = 0; i < systemList.Items.Count; i++)
                    {
                        if(systemList.Items[i] != systemList.SelectedItem){
                            index = i;
                            break;
                        }
                    }
                        
                    if(index != -1){
                        App.Singleton.xSelectSystem((string)systemList.Items[0]);
                        systemList.Enabled = true;
                    }                   
                }
                else
                {
                    // no more systems left
                    tabBox.TabPages.Remove(affectorTab);
                    tabBox.TabPages.Remove(emitterTab);
                    tabBox.TabPages.Remove(extraTab);
                    systemList.Enabled = false;
                    
                    // deselect
                    emitterList.Enabled = false;
                    delSystemBtn.Enabled = false;
                    btnAddEmitter.Enabled = false;
                    delEmitterBtn.Enabled = false;
                    btnUpdate.Enabled = false;
                    App.Singleton.xSelectSystem((ParticleSystem)null);
                    App.Singleton.xSelectEmitter((ParticleEmitter)null);
                    App.Singleton.xSelectAffector(null);
                }
            }

            UpdateSystemInfo(true, true);
            UpdateAffectorValues(true);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ParticleSystem ps = App.Singleton.xGetCurrentSystem();
            if (ps == null) return;

            if(emitterList.SelectedIndex >= 0)
            {

                ParticleEmitter em = ps.GetEmitter((ushort)emitterList.SelectedIndex);
                App.Singleton.xRemoveEmitterModels(em.Name); // cleeanup Ogre models
                ps.RemoveEmitter((ushort)emitterList.SelectedIndex);
                

                if(ps.NumEmitters > 0){
                    App.Singleton.xSelectEmitter(ps.GetEmitter(0));
                }else{
                    App.Singleton.xSelectEmitter((ParticleEmitter)null);
                    tabBox.TabPages.Remove(emitterTab);
                    delEmitterBtn.Enabled = false;
                }
                UpdateParamTab(App.Singleton.xGetCurrentEmitter());
                UpdateSystemInfo(false, true);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveDlg.DefaultExt = "flow";
            saveDlg.Filter = "Flow Config (*.flow)|*.flow";
            saveDlg.FileName = "";
            if(saveDlg.ShowDialog() == DialogResult.OK)
            {
                App.Singleton.xSaveConfinguration(saveDlg.FileName);
            }
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            App.Singleton.ShutDown();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            saveDlg.DefaultExt = "particle";
            saveDlg.Filter = "Ogre particle script (*.particle)|*.particle";
            saveDlg.FileName = "";
            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                App.Singleton.xExportScript(saveDlg.FileName);
            }
            
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openDlg.Filter = "Flow Config (*.flow)|*.flow";
            openDlg.DefaultExt = "flow.xml";
            openDlg.FileName = "";
            if(openDlg.ShowDialog() == DialogResult.OK){
                App.Singleton.xLoadConfinguration(openDlg.FileName);
                this.UpdateSystemInfo(true, true);
                this.UpdateEmitterValues(App.Singleton.xGetCurrentEmitter());
                this.UpdateAffectorValues(true);
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            App.Singleton.xClearEverything();
            this.UpdateSystemInfo(true, true);
            this.UpdateEmitterValues(App.Singleton.xGetCurrentEmitter());
            this.UpdateAffectorValues(true);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Ogre Flow Particle Editor\nVersion 1.0 Build: 2011-06-08 22:13\n" +
                "Created by: Tomas Uktveris\n\nCopyright© " + DateTime.Now.Year+"\nhttp://wzona.blogspot.com","About");
        }

    }
}
