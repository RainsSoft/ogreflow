using System;
using System.Collections.Generic;
using System.Collections;
using Mogre;
using System.Runtime.InteropServices;
using System.Xml.XPath;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.IO;

namespace GameX
{
    public class App
    {
        const int VERSION = 1000;

        public class Param
        {
            public string Property { get; set; }
            public string Value { get; set; }
            public string Description { get; set; }
        }

        public static void ShowOgreException()
        {
            if (OgreException.IsThrown)
                System.Windows.Forms.MessageBox.Show(OgreException.LastException.FullDescription, 
                    "An exception has occured!", System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Error);
        }

        // settings

        private bool DebugMode {
            get { return true;  }
        }

        private bool UseBufferedInput {
            get { return true; }
        }

        // set to true - to lock the mouse automatically by MOIS
        private bool AutoLockMouse 
        {
            get { return false; }
        }

        private int emitterId = 0;
        private int systemId = 0;
        private float time = 0;
        private ParticleSystem psystem = null;
        private ParticleEmitter pemitter = null;
        private ParticleAffector paffector = null;
        private List<ParticleSystem> plist = new List<ParticleSystem>();

        private bool running = true;
        private Root root;
        private Camera camera;
        private Viewport viewport;
        private SceneManager sceneMgr;
        private RenderWindow window;
        private MOIS.InputManager inputManager;
        private MOIS.Keyboard inputKeyboard;
        private MOIS.Mouse inputMouse;

        private Overlay debugOverlay;
        private String mDebugText = "";
        private static IntPtr WindowHandle;

        //Do NOT call root.Dispose at the finalizer thread because GL renderer requires that disposing of its objects is made
        //in the same thread as the thread that created the GL renderer.
        //~ExampleApplication()
        //{
        //    if (root != null)
        //    {
        //        root.Dispose();
        //        root = null;
        //    }
        //}

        // custom imports
        [DllImport("user32.dll")]
        public static extern int ShowCursor(bool bShow);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(HandleRef hwnd, out RECT lpRect);

        public void Go()
        {
            // show toolbox
            if (DebugMode){
                (new Toolbox()).Show();
            }

            if (!Setup()) return;

            root.StartRendering();

            // clean up
            DestroyScene();

            root.Dispose();
            root = null;
        }

        public void ShutDown(){
            running = false;
        }

        private bool Setup()
        {
            root = new Root();

            SetupResources();

            bool carryOn = Configure(true);
            if (!carryOn) return false;

            ChooseSceneManager();
            CreateCamera();
            CreateViewports();

            // Set default mipmap level (NB some APIs ignore this)
            TextureManager.Singleton.DefaultNumMipmaps = 5;

            // Create any resource listeners (for loading screens)
            CreateResourceListener();
            // Load resources
            LoadResources();

            // Create the scene
            CreateScene();

            CreateFrameListener();

            CreateInput();

            return true;

        }

        private bool Configure(bool trySkip)
        {
            if (trySkip)
            {
                if(root.RestoreConfig())
                {
                    window = root.Initialise(true);
                    return true;
                }
            }

            // Show the configuration dialog and initialise the system
            // You can skip this and use root.restoreConfig() to load configuration
            // settings if you were sure there are valid ones saved in ogre.cfg
            if (root.ShowConfigDialog())
            {
                // If returned true, user clicked OK so initialise
                // Here we choose to let the system create a default rendering window by passing 'true'
                window = root.Initialise(true);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ChooseSceneManager()
        {
            // Get the SceneManager, in this case a generic one
            sceneMgr = root.CreateSceneManager(SceneType.ST_GENERIC, "SceneMgr");
        }

        private void CreateCamera()
        {
            // Create the camera
            camera = sceneMgr.CreateCamera("PlayerCam");


            SceneNode node = sceneMgr.RootSceneNode.CreateChildSceneNode("camNode");
            node.AttachObject(camera);

            // Position it at 500 in Z direction
            camera.Position = new Vector3(0, 10, 20);
            camera.FOVy = (new Degree(60)).ValueRadians;
            // Look back along -Z
            camera.LookAt(new Vector3(0, 0, -10));
            camera.NearClipDistance = 0.1f; // see nearer
            camera.FarClipDistance = 2000;
        }

        private void CreateFrameListener()
        {
            debugOverlay = OverlayManager.Singleton.GetByName("Core/DebugOverlay");
            debugOverlay.Show();
            root.FrameStarted += new FrameListener.FrameStartedHandler(Update);
        }

        private bool Update(FrameEvent evt)
        {
            if (window.IsClosed || !running)
                return false;

            time += evt.timeSinceLastFrame;

            inputKeyboard.Capture();

            UpdateStats();

            return running;
        }

        private bool KeyPressed(MOIS.KeyEvent arg)
        {
            // stop
            if(arg.key == MOIS.KeyCode.KC_ESCAPE){
                running = false;
            }

            return true;
        }

        private bool KeyReleased(MOIS.KeyEvent arg)
        {
            return true;
        }

        private bool MouseMoved(MOIS.MouseEvent arg)
        {
            return true;
        }

        private bool MousePressed(MOIS.MouseEvent arg, MOIS.MouseButtonID id)
        {
            return true;
        }

        private bool MouseReleased(MOIS.MouseEvent arg, MOIS.MouseButtonID id)
        {
            return true;
        }

        public void TakeScreenshot()
        {
            string[] temp = System.IO.Directory.GetFiles(Environment.CurrentDirectory, "screenshot*.jpg");
            string fileName = string.Format("screenshot{0}.jpg", temp.Length + 1);

            window.WriteContentsToFile(fileName);
        }

        private void UpdateStats()
        {
            string currFps = "Current FPS: ";
            string avgFps = "Average FPS: ";
            string bestFps = "Best FPS: ";
            string worstFps = "Worst FPS: ";
            string tris = "Triangle Count: ";

            // update stats when necessary
            try
            {
                OverlayElement guiAvg = OverlayManager.Singleton.GetOverlayElement("Core/AverageFps");
                OverlayElement guiCurr = OverlayManager.Singleton.GetOverlayElement("Core/CurrFps");
                OverlayElement guiBest = OverlayManager.Singleton.GetOverlayElement("Core/BestFps");
                OverlayElement guiWorst = OverlayManager.Singleton.GetOverlayElement("Core/WorstFps");

                RenderTarget.FrameStats stats = window.GetStatistics();

                guiAvg.Caption = avgFps + stats.AvgFPS;
                guiCurr.Caption = currFps + stats.LastFPS;
                guiBest.Caption = bestFps + stats.BestFPS + " " + stats.BestFrameTime + " ms";
                guiWorst.Caption = worstFps + stats.WorstFPS + " " + stats.WorstFrameTime + " ms";

                OverlayElement guiTris = OverlayManager.Singleton.GetOverlayElement("Core/NumTris");
                guiTris.Caption = tris + stats.TriangleCount;

                OverlayElement guiDbg = OverlayManager.Singleton.GetOverlayElement("Core/DebugText");
                guiDbg.Caption = mDebugText;
            }
            catch
            {
                // ignore
            }
        }

        ////////////////////////////////////////////////////////

        public void xSelectSystem(ParticleSystem s)
        {
            psystem = s;
        }

        public void xSelectSystem(String name)
        {
            psystem = xGetSystemByName(name);
        }

        public ParticleSystem xAddParticleSystem()
        {
            // fetch unique name
            while (sceneMgr.HasParticleSystem("System" + systemId)) { 
                systemId++; 
            }

            return xAddParticleSystem("System" + systemId++);
        }

        public ParticleSystem xAddParticleSystem(string name)
        {
            ParticleSystem ps = sceneMgr.CreateParticleSystem(name);
            sceneMgr.RootSceneNode.CreateChildSceneNode(ps.Name).AttachObject(ps);
            ps.SetMaterialName("ParticleDefault");
            ps.SetDefaultDimensions(1, 1);

            plist.Add(ps);
            return ps;
        }

        public ParticleSystem xGetCurrentSystem(){
            return psystem;
        }

        public ParticleEmitter xGetCurrentEmitter()
        {
            return pemitter;
        }

        public ParticleAffector xGetCurrentAffector()
        {
            return paffector;
        }

        public ParticleSystem xGetSystemByName(String name)
        {
            for (int i = 0; i < plist.Count; i++)
            {
                if (plist[i].Name == name)
                {
                    return plist[i];
                }
            }

            return null;
        }


        public void xResetCurrSystem()
        {

        }

        public string[] xGetSystemNames()
        {
            string[] names = new string[plist.Count];
            for(int i=0; i<plist.Count; i++)
            {
                ParticleSystem p = plist[i];
                names[i] = p.Name;
            }
            return names;
        }

        public string[] xGetEmitterNames()
        {
            if (psystem == null) return null;

            string[] names = new string[psystem.NumEmitters];
            for (int i = 0; i < psystem.NumEmitters; i++)
            {
                ParticleEmitter pe = psystem.GetEmitter((ushort)i);
                names[i] = pe.Name;
            }
            return names;
        }

        public string[] xGetEmitterTypes()
        {
            List<string> names = new List<string>();

            try{
                XPathDocument doc = new XPathDocument("../media/materials/emitters.xml");
                XPathNavigator xp = doc.CreateNavigator();

                XPathNodeIterator it = xp.Select("//@emitter");

                while(it.MoveNext()){
                    names.Add(it.Current.Value);
                }

            }catch(Exception err){
                Console.WriteLine("Error reading emitters.xml: " + err);
            }

            return names.ToArray();
        }

        public Dictionary<string, string> xGetDefEmitterParams(string emitterType){

            Dictionary<string, string> par = new Dictionary<string, string>();

            try
            {
                XPathDocument doc = new XPathDocument("../media/materials/emitters.xml");
                XPathNavigator xp = doc.CreateNavigator();

                string xpath = String.Format("//add[@emitter='{0}']/param", emitterType);
                XPathNodeIterator it = xp.Select(xpath);

                while (it.MoveNext())
                {
                    string name = it.Current.GetAttribute("name", "");
                    string defval = it.Current.GetAttribute("default", "");
                    par.Add(name, defval);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Error reading emitters.xml: " + err);
            }

            return par;
        }

        public List<Param> xGetExtraEmitterParams(ParticleEmitter pe)
        {

            List<Param> list = new List<Param>();

            if (pe == null) return list;

            // the default
            Dictionary<string, string> def = xGetDefEmitterParams(pe.Type);

            //////////////////////////////////////////////////////////////////////////
            Const_ParameterList plist = pe.GetParameters();
            foreach (ParameterDef_NativePtr c in plist)
            {
                // dont include base emitter properties!
                if (def.ContainsKey(c.name) == false) continue;

                string value = pe.GetParameter(c.name);
                list.Add(new Param() { Property = c.name, Value = value, Description = c.description });
            }

            return list;
        }

        public List<Param> xGetExtraAffectorParams(ParticleAffector af)
        {

            List<Param> list = new List<Param>();

            if (af == null) return list;

            // the default
            //Dictionary<string, string> def = xGetDefEmitterParams(pe.Type);

            //////////////////////////////////////////////////////////////////////////
            Const_ParameterList plist = af.GetParameters();
            foreach (ParameterDef_NativePtr c in plist)
            {
                // dont include base emitter properties!
                //if (def.ContainsKey(c.name) == false) continue;

                string value = af.GetParameter(c.name);
                list.Add(new Param() { Property = c.name, Value = value, Description = c.description });
            }

            return list;
        }

        public string[] xGetBillboardTypes()
        {
            List<string> names = new List<string>();

            try
            {
                XPathDocument doc = new XPathDocument("../media/materials/billboards.xml");
                XPathNavigator xp = doc.CreateNavigator();
                XPathNodeIterator it = xp.Select("//@name");

                while (it.MoveNext())
                {
                    names.Add(it.Current.Value);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading billboards.xml: " + e);
            }

            return names.ToArray();
        }

        public string[] xGetParticleMaterials()
        {
            List<string> names = new List<string>();

            try
            {
                XPathDocument doc = new XPathDocument("../media/materials/mats.xml");
                XPathNavigator xp = doc.CreateNavigator();
                XPathNodeIterator it = xp.Select("//@name");

                while (it.MoveNext())
                {
                    names.Add(it.Current.Value);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error reading mats.xml: " + e);
            }

            return names.ToArray();
        }

        public string[] xGetAffectorTypes()
        {
            List<string> names = new List<string>();

            try
            {
                XPathDocument doc = new XPathDocument("../media/materials/affectors.xml");
                XPathNavigator xp = doc.CreateNavigator();
                XPathNodeIterator it = xp.Select("//@name");

                while (it.MoveNext())
                {
                    names.Add(it.Current.Value);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading affectors.xml: " + e);
            }

            return names.ToArray();
        }

        public void xRemoveEmitterModels(string name)
        {
            try
            {
                SceneNode node = sceneMgr.GetSceneNode(name);
                node.DetachAllObjects();
                sceneMgr.DestroySceneNode(node);
                sceneMgr.DestroyManualObject(name); // destroy dummy model
            }
            catch(Exception err)
            {
                MessageBox.Show("Error: "+err);
            }
        }

        public void xRemoveSystem(ParticleSystem ps)
        {
            if (ps == null) return;

            if(psystem == ps){

                // destroy emitter model data
                for(int i=0; i<ps.NumEmitters; i++){
                    xRemoveEmitterModels(ps.GetEmitter((ushort)i).Name);
                }

                // free model data
                SceneNode node = sceneMgr.GetSceneNode(ps.Name);
                node.DetachAllObjects();
                sceneMgr.DestroySceneNode(node);
                sceneMgr.DestroyParticleSystem(ps);
            }

            plist.Remove(ps);
        }

        public ParticleEmitter xAddTypedEmitter(string type, Dictionary<string, string> defParams)
        {
            ParticleEmitter em = null;

            try
            {
                em = psystem.AddEmitter(type);
                em.Name = "Emitter" + emitterId++ + ":"+type;
                em.SetColour(new ColourValue(1, 1, 1, 1), new ColourValue(1, 1, 1, 0));
                em.SetDuration(5, 10);
                em.SetEmitted(false);
                em.SetParticleVelocity(1, 2);
                em.SetRepeatDelay(0, 0.1f);
                em.SetTimeToLive(0, 10);
                em.Position = new Vector3(0, 3, 0);
                em.Direction = new Vector3(0, 1, 0);
                em.Angle = (new Degree(30)).ValueRadians;
                em.EmissionRate = 100;
                em.StartTime = 0;

                // apply params
                if (defParams != null)
                {
                    foreach (KeyValuePair<string, string> p in defParams)
                    {
                        em.SetParameter(p.Key, p.Value);
                    }
                }

                // create dummy
                MovableObject dummy = Utils.MakeDummy(sceneMgr, em.Name, "Dummy", 1);
                SceneNode node = sceneMgr.RootSceneNode.CreateChildSceneNode(em.Name);
                node.AttachObject(dummy);
                node.Position = em.Position;
               
            }catch(Exception err){
                MessageBox.Show("Error: Invalid emitter settings" + err);
            }

            return em;
        }

        public ParticleEmitter xGetEmitterByName(String name)
        {
            for (int i = 0; i < psystem.NumEmitters; i++)
            {
                ParticleEmitter pe = psystem.GetEmitter((ushort)i);
                if (pe.Name == name)
                {
                    return pe;
                }
            }

            return null;
        }

        public void xSelectEmitter(ParticleEmitter e)
        {
            pemitter = e;
        }

        public void xSelectEmitter(String name)
        {
            pemitter = xGetEmitterByName(name);
        }

        public void xSelectAffector(ParticleAffector af)
        {
            paffector = af;
        }

        public void xUpdateEmitter(ParticleEmitter pe){
            if (pe == null) return;

            // update dummy model
            if (pemitter != null)
            {
                SceneNode node = sceneMgr.GetSceneNode(pemitter.Name);
                node.Position = pe.Position;
            }
        }

        public float xGetSystemTime(){
            return (float)time;
        }

        public void xRotateCamera(float amount)
        {
            Radian r = (new Degree(amount)).ValueRadians;
            SceneNode node = sceneMgr.GetSceneNode("camNode");
            node.Yaw(r, Node.TransformSpace.TS_WORLD);
        }

        public void xZoomCamera(float fovangle)
        {
            Radian r = (new Degree(fovangle)).ValueRadians;
            camera.FOVy = r;
        }

        // show/hide skybox
        public void xToggleSkybox(bool state)
        {
            if (state == false)
            {
                sceneMgr.SetSkyBox(false, "");
            }
            else
            {
                sceneMgr.SetSkyBox(true, "SimpleSky", 1000);
            }
        }

        // show/hide emitter octahedron models
        public void xToggleEmitterModel(bool state)
        {
            foreach(ParticleSystem p in plist)
            {
                for(int i=0; i<p.NumEmitters; i++){
                    ParticleEmitter em = p.GetEmitter((ushort)i);
                    SceneNode node = sceneMgr.GetSceneNode(em.Name);
                    if(node != null){
                        node.SetVisible(state);
                    }
                }
            }
        }

        public ParticleAffector xAddAffector(string type)
        {
            ParticleSystem ps = xGetCurrentSystem();
            if(ps == null) return null;
            return ps.AddAffector(type);
        }

        // export ogre particle script
        public void xExportScript(string filename)
        {
            StreamWriter fp = null;
            try
            {
                fp = File.CreateText(filename);
            }
            catch (Exception err)
            {
                MessageBox.Show("Error: " + err.Message);
                return;
            }

            for (int i = 0; i < plist.Count; i++ )
            {
                ParticleSystem ps = plist[i];
                fp.WriteLine("particle_system {0}", ps.Name);
                fp.WriteLine("{");

                fp.WriteLine("\tmaterial {0}", ps.MaterialName);
                fp.WriteLine("\tparticle_width {0}", ps.DefaultWidth);
                fp.WriteLine("\tparticle_height {0}", ps.DefaultHeight);
                fp.WriteLine("\tcull_each {0}", ps.CullIndividually);
                fp.WriteLine("\tquota {0}", ps.ParticleQuota);
                fp.WriteLine("\tsorted {0}", ps.SortingEnabled);
                fp.WriteLine("\tbillboard_type {0}", ps.Renderer.GetParameter("billboard_type"));
                fp.WriteLine("\tcommon_direction {0}", ps.Renderer.GetParameter("common_direction"));
                fp.WriteLine("\tcommon_up_vector {0}", ps.Renderer.GetParameter("common_up_vector"));
                fp.WriteLine();

                // emitters
                for (int j = 0; j < ps.NumEmitters; j++ )
                {
                    ParticleEmitter pe = ps.GetEmitter((ushort)j);
                    fp.WriteLine("\temitter {0}", pe.Type);
                    fp.WriteLine("\t{");

                    foreach (ParameterDef_NativePtr param in pe.GetParameters())
                    {
                        string value = pe.GetParameter(param.name);
                        if (value == "") continue; // no empty
                        if (param.name == "name") continue; 
                        fp.WriteLine("\t\t{0} {1}", param.name, value);
                    }

                    fp.WriteLine("\t}");
                    fp.WriteLine();
                }

                // affectors
                for (int j = 0; j < ps.NumAffectors; j++)
                {
                    ParticleAffector af = ps.GetAffector((ushort)j);
                    fp.WriteLine("\taffector {0}", af.Type);
                    fp.WriteLine("\t{");

                    foreach (ParameterDef_NativePtr param in af.GetParameters())
                    {
                        string value = af.GetParameter(param.name);
                        fp.WriteLine("\t\t{0} {1}", param.name, value);
                    }

                    fp.WriteLine("\t}");
                    fp.WriteLine();
                }

                fp.WriteLine("}");
                fp.WriteLine();
            }

            fp.Close();
        }

        // clear existing particle systems
        public void xClearEverything()
        {
            while (plist.Count > 0)
            {
                this.xRemoveSystem(plist[0]);
                //plist.RemoveAt(0); // not needed handles in xRemoveSystem() 
            }

            sceneMgr.DestroyAllParticleSystems(); // just to make sure all is clear

            psystem = null;
            pemitter = null;
            paffector = null;
        }

        // save flow file
        public void xSaveConfinguration(string filename)
        {
            XmlTextWriter xml = null;

            try
            {
                xml = new XmlTextWriter(filename, null);
            }
            catch(Exception err)
            {
                MessageBox.Show("Error: " + err.Message);
                return;
            }

            xml.Formatting = Formatting.Indented; // tabs & newlines
            xml.WriteStartDocument();
            {
                xml.WriteStartElement("config");
                    xml.WriteStartAttribute("version");
                    xml.WriteValue(1000);
                    xml.WriteEndAttribute();

                    for(int i=0; i<plist.Count; i++)
                    {
                        ParticleSystem p = plist[i];
                        xml.WriteStartElement("system");
                            xml.WriteStartAttribute("name");
                            xml.WriteValue(p.Name);
                            xml.WriteEndAttribute();

                            xml.WriteStartElement("params");
                                foreach (ParameterDef_NativePtr param in p.GetParameters())
                                {
                                    xml.WriteStartElement("add");
                                        xml.WriteStartAttribute("key");
                                        xml.WriteValue(param.name);
                                        xml.WriteEndAttribute();

                                        xml.WriteStartAttribute("value");
                                        xml.WriteValue(p.GetParameter(param.name));
                                        xml.WriteEndAttribute();
                                    xml.WriteEndElement();
                                }

                                // billboard type
                                xml.WriteStartElement("add");
                                xml.WriteStartAttribute("key");
                                xml.WriteValue("billboard_type");
                                xml.WriteEndAttribute();

                                xml.WriteStartAttribute("value");
                                xml.WriteValue(p.Renderer.GetParameter("billboard_type"));
                                xml.WriteEndAttribute();
                                xml.WriteEndElement();

                                // common dir
                                xml.WriteStartElement("add");
                                xml.WriteStartAttribute("key");
                                xml.WriteValue("common_direction");
                                xml.WriteEndAttribute();

                                xml.WriteStartAttribute("value");
                                xml.WriteValue(p.Renderer.GetParameter("common_direction"));
                                xml.WriteEndAttribute();
                                xml.WriteEndElement();
                            xml.WriteEndElement();

                        // affectors
                        for (int j = 0; j < p.NumAffectors; j++ )
                        {
                            ParticleAffector af = p.GetAffector((ushort)j);
                            
                            xml.WriteStartElement("affector");
                            xml.WriteStartAttribute("type");
                            xml.WriteValue(af.Type);
                            xml.WriteEndAttribute();

                            xml.WriteStartAttribute("name");
                            xml.WriteValue("Affector"+j);
                            xml.WriteEndAttribute();
                            {
                                xml.WriteStartElement("params");
                                foreach (ParameterDef_NativePtr param in af.GetParameters())
                                {
                                    xml.WriteStartElement("add");
                                        xml.WriteStartAttribute("key");
                                        xml.WriteValue(param.name);
                                        xml.WriteEndAttribute();

                                        xml.WriteStartAttribute("value");
                                        xml.WriteValue(af.GetParameter(param.name));
                                        xml.WriteEndAttribute();
                                    xml.WriteEndElement();
                                }
                                xml.WriteEndElement();
                            }
                            xml.WriteEndElement();
                        }

                        // emitters
                        for (int j = 0; j < p.NumEmitters; j++)
                        {
                            ParticleEmitter em = p.GetEmitter((ushort)j);
                            xml.WriteStartElement("emitter");
                            xml.WriteStartAttribute("type");
                            xml.WriteValue(em.Type);
                            xml.WriteEndAttribute();

                            xml.WriteStartAttribute("name");
                            xml.WriteValue("Emitter" + j);
                            xml.WriteEndAttribute();
                            {
                                xml.WriteStartElement("params");
                                foreach (ParameterDef_NativePtr param in em.GetParameters())
                                {
                                    xml.WriteStartElement("add");
                                        xml.WriteStartAttribute("key");
                                        xml.WriteValue(param.name);
                                        xml.WriteEndAttribute();

                                        xml.WriteStartAttribute("value");
                                        xml.WriteValue(em.GetParameter(param.name));
                                        xml.WriteEndAttribute();
                                    xml.WriteEndElement();
                                }
                                xml.WriteEndElement();
                            }
                            xml.WriteEndElement();
                        }

                        xml.WriteEndElement(); // </system>
                    }
                    
                xml.WriteEndElement();//</config>
            }
            xml.WriteEndDocument();
            xml.Close();
        }

        // load flow file
        public void xLoadConfinguration(string filename)
        {

            this.xClearEverything(); 

            XPathDocument doc = null;

            try
            {
                doc = new XPathDocument(filename);
            }
            catch (Exception err)
            {
                MessageBox.Show("Error: " + err.Message);
                return;
            }

            XPathNavigator nav = doc.CreateNavigator();
            
            try
            {
                XPathNodeIterator it = nav.Select("/config");
                it.MoveNext();
                string version = it.Current.GetAttribute("version", "");

                // get all systems
                XPathNodeIterator sysnames = nav.Select("//system/@name");

                while (sysnames.MoveNext())
                {
                    string sysName = sysnames.Current.Value;
                    ParticleSystem ps = this.xAddParticleSystem(sysName);
                    this.xSelectSystem(ps);

                    // get system params
                    string path = String.Format("//system[@name='{0}']/params/add", sysName);
                    XPathNodeIterator param = nav.Select(path);
                    while(param.MoveNext())
                    {
                        string pName = param.Current.GetAttribute("key","");
                        string pValue = param.Current.GetAttribute("value","");
                        ps.SetParameter(pName, pValue);
                    }

                    // create affectors
                    string affpath = String.Format("//system[@name='{0}']/affector", sysName);
                    XPathNodeIterator affit = nav.Select(affpath);

                    while(affit.MoveNext())
                    {
                        string af_type = affit.Current.GetAttribute("type", "");
                        string af_name = affit.Current.GetAttribute("name", "");
                        ParticleAffector af = this.xAddAffector(af_type);

                        string affparam = String.Format(
                            "//system[@name='{0}']/affector[@name='{1}']/params/add", 
                            sysName, af_name);
                        XPathNodeIterator pit = nav.Select(affparam);
                        while(pit.MoveNext())
                        {
                            string pName = pit.Current.GetAttribute("key","");
                            string pValue = pit.Current.GetAttribute("value","");
                            af.SetParameter(pName, pValue);
                        }
                    }

                    // create emitters
                    string empath = String.Format("//system[@name='{0}']/emitter", sysName);
                    XPathNodeIterator emit = nav.Select(empath);

                    while (emit.MoveNext())
                    {
                        string em_type = emit.Current.GetAttribute("type", "");
                        string em_name = emit.Current.GetAttribute("name", "");
                        ParticleEmitter em = this.xAddTypedEmitter(em_type, 
                            this.xGetDefEmitterParams(em_type));

                        string emparam = String.Format(
                            "//system[@name='{0}']/emitter[@name='{1}']/params/add",
                            sysName, em_name);
                        XPathNodeIterator pit = nav.Select(emparam);
                        while (pit.MoveNext())
                        {
                            string pName = pit.Current.GetAttribute("key", "");
                            if (pName == "name") continue; // do not overwrite name!
                            string pValue = pit.Current.GetAttribute("value", "");
                            em.SetParameter(pName, pValue);
                        }
                    }
                }
            }
            catch(Exception err)
            {
                MessageBox.Show("Error: " + err);
            }

            // select first
            if(plist.Count>0)
            {
                this.xSelectSystem(plist[0]);
                if(plist[0] != null && plist[0].NumEmitters>0){
                    this.xSelectEmitter(plist[0].GetEmitter(0));
                }
            }
           
        }

        ////////////////////////////////////////////////////////

        private void CreateScene()
        {
            window.SetDeactivateOnFocusChange(false);

            // load all data here!
            this.xToggleSkybox(true);

            //Light sun = sceneMgr.CreateLight();
            //sun.Type = Light.LightTypes.LT_DIRECTIONAL;
            //sun.Direction = (new Vector3(-1, -1, 0)).NormalisedCopy;
            //sun.SetDiffuseColour(1, 1, 1);

            // example plane
            const float MAP_SZ = 100;
            Entity plane = Utils.MakePlane(sceneMgr, MAP_SZ, 20);
            plane.SetMaterialName("PlaneGround");
            sceneMgr.RootSceneNode.AttachObject(plane);

            //sceneMgr.ShowBoundingBoxes = true;
        }

        private void DestroyScene()
        {

        }    


        private void CreateInput()
        {
            LogManager.Singleton.LogMessage("*** Initializing OIS ***");
            MOIS.ParamList pl = new MOIS.ParamList();
            
            if (AutoLockMouse == false)
            {
                pl.Insert("w32_mouse", "DISCL_FOREGROUND");
                pl.Insert("w32_mouse", "DISCL_NONEXCLUSIVE");
            }
            window.GetCustomAttribute("WINDOW", out WindowHandle);
            pl.Insert("WINDOW", WindowHandle.ToString());

            inputManager = MOIS.InputManager.CreateInputSystem(pl);

            //Create all devices (We only catch joystick exceptions here, as, most people have Key/Mouse)
            inputKeyboard = (MOIS.Keyboard)inputManager.CreateInputObject(MOIS.Type.OISKeyboard, UseBufferedInput);
            inputMouse = (MOIS.Mouse)inputManager.CreateInputObject(MOIS.Type.OISMouse, UseBufferedInput);

            if (inputKeyboard != null)
            {
                LogManager.Singleton.LogMessage("Setting up keyboard listeners");
                inputKeyboard.KeyPressed += new MOIS.KeyListener.KeyPressedHandler(KeyPressed);
                inputKeyboard.KeyReleased += new MOIS.KeyListener.KeyReleasedHandler(KeyReleased);
            }

            if (inputMouse != null)
            {
                LogManager.Singleton.LogMessage("Setting up mouse listeners");
                inputMouse.MousePressed += new MOIS.MouseListener.MousePressedHandler(MousePressed);
                inputMouse.MouseReleased += new MOIS.MouseListener.MouseReleasedHandler(MouseReleased);
                inputMouse.MouseMoved += new MOIS.MouseListener.MouseMovedHandler(MouseMoved);
            }
        }

        private void CreateViewports()
        {
            // Create one viewport, entire window
            viewport = window.AddViewport(camera);
            viewport.BackgroundColour = new ColourValue(0, 0, 0);
            // Alter the camera aspect ratio to match the viewport
            camera.AspectRatio = ((float)viewport.ActualWidth) / ((float)viewport.ActualHeight);
        }

        /// Method which will define the source of resources (other than current folder)
        private void SetupResources()
        {
            // Load resource paths from config file
            ConfigFile cf = new ConfigFile();
            cf.Load("../resources.cfg", "\t:=", true);

            // Go through all sections & settings in the file
            ConfigFile.SectionIterator seci = cf.GetSectionIterator();

            String secName, typeName, archName;

            // Normally we would use the foreach syntax, which enumerates the values, but in this case we need CurrentKey too;
            while (seci.MoveNext())
            {
                secName = seci.CurrentKey;
                ConfigFile.SettingsMultiMap settings = seci.Current;
                foreach (KeyValuePair<string, string> pair in settings)
                {
                    typeName = pair.Key;
                    archName = pair.Value;
                    ResourceGroupManager.Singleton.AddResourceLocation(archName, typeName, secName);
                }
            }
        }

        /// Optional override method where you can create resource listeners (e.g. for loading screens)
        private void CreateResourceListener()
        {

        }

        /// Optional override method where you can perform resource group loading
        /// Must at least do ResourceGroupManager.Singleton.InitialiseAllResourceGroups();
        private void LoadResources()
        {
            // Initialise, parse scripts etc
            ResourceGroupManager.Singleton.InitialiseAllResourceGroups();
        }

        public static App Singleton { get; set; }

        [STAThread]
        public static void Main(){
            Singleton = new App();
            Singleton.Go();
        }
    }
}
