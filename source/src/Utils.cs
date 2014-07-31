using System;
using System.Collections.Generic;
using System.Text;
using Mogre;

namespace GameX
{
    public class Utils
    {

        // pack/unpack vector to a string
        public static String pack3(Vector3 v){
            return v.x.ToString() + "," + v.y.ToString() + "," + v.z.ToString();
        }

        public static Vector3 unpack3(String s)
        {
            string[] args = s.Split(new char[]{ ',' });
            return new Vector3(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]));
        }

        public static String packq(Quaternion q)
        {
            return q.x.ToString() + "," + q.y.ToString() + "," + q.z.ToString() + "," + q.w.ToString();
        }

        public static Quaternion unpackq(String s)
        {
            string[] args = s.Split(new char[] { ',' });
            return new Quaternion(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]), float.Parse(args[3]));
        }

        public static String pack2(Vector2 v)
        {
            return v.x.ToString() + "," + v.y.ToString();
        }

        public static Vector2 unpack2(String s)
        {
            string[] args = s.Split(new char[] { ',' });
            return new Vector2(float.Parse(args[0]), float.Parse(args[1]));
        }

        // create grid
        public static void CreateGrid(SceneManager mgr, String name, int numcols, int numrows, float unitsize)
        {
            ManualObject grid = mgr.CreateManualObject(name);

            grid.Begin("BaseWhiteNoLighting", RenderOperation.OperationTypes.OT_LINE_LIST);

            float width = (float)numcols * unitsize;
            float depth = (float)numrows * unitsize;
            Vector3 center = new Vector3(-width / 2.0f, 0, -depth / 2.0f);

            for (int i = 0; i < numrows; ++i)
            {
                Vector3 s, e;
                s.x = 0.0f;
                s.z = i * unitsize;
                s.y = 0.0f;

                e.x = width;
                e.z = i * unitsize;
                e.y = 0.0f;

                grid.Position(s + center);
                grid.Position(e + center);
            }
            grid.Position(new Vector3(0.0f, 0.0f, numrows * unitsize) + center);
            grid.Position(new Vector3(width, 0.0f, numrows * unitsize) + center);

            for (int i = 0; i < numcols; ++i)
            {
                Vector3 s, e;
                s.x = i * unitsize;
                s.z = depth;
                s.y = 0.0f;

                e.x = i * unitsize;
                e.z = 0.0f;
                e.y = 0.0f;

                grid.Position(s + center);
                grid.Position(e + center);
            }
            grid.Position(new Vector3(numcols * unitsize, 0.0f, 0.0f) + center);
            grid.Position(new Vector3(numcols * unitsize, 0.0f, depth) + center);
            grid.End();

            mgr.RootSceneNode.AttachObject(grid);
        }

        // creates a single Y plane
        public static Entity MakePlane(SceneManager mgr, float size, float tile)
        {
            MeshPtr mesh = MeshManager.Singleton.CreatePlane("ground", 
                ResourceGroupManager.DEFAULT_RESOURCE_GROUP_NAME,
                new Plane(Vector3.UNIT_Y, 0), 
                size, size, 1, 1, true, 1, tile, tile, Vector3.UNIT_Z);

            // Create a ground plane
            return mgr.CreateEntity("plane", "ground");
        }

        // creates a simple box with specified material
        public static MovableObject MakeBox(SceneManager mgr, String materialName, Vector3 dims, Vector2 coord)
        {
            ManualObject mo = mgr.CreateManualObject();
            mo.Begin(materialName, RenderOperation.OperationTypes.OT_TRIANGLE_LIST);
            
            float w = dims.x * 0.5f;
            float h = dims.y * 0.5f;
            float d = dims.z * 0.5f;

            Vector3[] norm = {
                new Vector3( 1.0f, 0.0f, 0.0f),
                new Vector3( 0.0f, 1.0f, 0.0f),
                new Vector3( 0.0f, 0.0f, 1.0f),
                new Vector3(-1.0f, 0.0f, 0.0f),
                new Vector3( 0.0f,-1.0f, 0.0f),
                new Vector3( 0.0f, 0.0f,-1.0f)
            };

            Vector3[] geom = { // 6 faces x 4 vertexes 
                new Vector3(+w,-h,+d), new Vector3(+w,-h,-d), new Vector3(+w,+h,-d), new Vector3(+w,+h,+d),
                new Vector3(+w,+h,+d), new Vector3(+w,+h,-d), new Vector3(-w,+h,-d), new Vector3(-w,+h,+d),
                new Vector3(+w,+h,+d), new Vector3(-w,+h,+d), new Vector3(-w,-h,+d), new Vector3(+w,-h,+d),
                new Vector3(-w,-h,+d), new Vector3(-w,+h,+d), new Vector3(-w,+h,-d), new Vector3(-w,-h,-d),
                new Vector3(-w,-h,+d), new Vector3(-w,-h,-d), new Vector3(+w,-h,-d), new Vector3(+w,-h,+d),
                new Vector3(-w,-h,-d), new Vector3(-w,+h,-d), new Vector3(+w,+h,-d), new Vector3(+w,-h,-d)
            };

            // texcoords
            Vector2[] uvs = { new Vector2(0, 0), new Vector2(0, coord.y), coord, new Vector2(coord.x, 0) };

            for(int i=0; i<6; i++){ // 6 faces
                uint k = (uint)(i * 4);
                for(int j=0; j<4; j++){ // 4 verts
                    mo.Position(geom[k+j]);
                    mo.Normal(norm[i]);
                    mo.TextureCoord(uvs[j]);
                }
                mo.Quad(k,k+1,k+2,k+3);
            }
            
            mo.End();

            return mo;
        }

        // create a 2D hud element: pos = [-1;1] & size = (pixels)
        public static MovableObject MakeHud(SceneManager mgr, Viewport vp, String materialName, Vector2 pos, Vector2 size)
        {
            // Create a manual object for 2D
            ManualObject manual = mgr.CreateManualObject();
            // Use identity view/projection matrices
            manual.UseIdentityProjection = true;
            manual.UseIdentityView = true;

            // convert from pixels to screen coords
            float s = size.x / vp.ActualWidth;
            float t = size.y / vp.ActualHeight;
             
            manual.Begin(materialName, RenderOperation.OperationTypes.OT_TRIANGLE_STRIP);

            manual.Position(pos.x - s, pos.y - t, 0.0f); manual.TextureCoord(0, 1);
            manual.Position(pos.x + s, pos.y - t, 0.0f); manual.TextureCoord(1, 1);
            manual.Position(pos.x + s, pos.y + t, 0.0f); manual.TextureCoord(1, 0);
            manual.Position(pos.x - s, pos.y + t, 0.0f); manual.TextureCoord(0, 0);
             
            manual.Index(0);
            manual.Index(1);
            manual.Index(2);
            manual.Index(3);
            manual.Index(0);
             
            manual.End();
             
            // Use infinite AAB to always stay visible
            AxisAlignedBox aabInf = new AxisAlignedBox();
            aabInf.SetInfinite();
            manual.BoundingBox = aabInf;
             
            // Render just before overlays
            manual.RenderQueueGroup = (byte)(RenderQueueGroupID.RENDER_QUEUE_OVERLAY-1);
            manual.CastShadows = false;
             
            // Attach to scene
            mgr.RootSceneNode.CreateChildSceneNode().AttachObject(manual);
            return manual;
        }

        // intersects objects in scene with ray from screen coords: x, y
        public static RaySceneQueryResult Pick(SceneManager mgr, int x, int y, Viewport vp, Camera cam)
        {
            //normalise mouse coordinates to [0,1]
            //we could have used the panel's width/height in pixels instead of viewport's width/height
            float scrx = (float)x / vp.ActualWidth;
            float scry = (float)y / vp.ActualHeight;

            Ray ray = cam.GetCameraToViewportRay(scrx, scry);
            RaySceneQuery query = mgr.CreateRayQuery(ray);
            RaySceneQueryResult results = query.Execute();
            return results;
        }

        // creates a wireframe octahedron as a dummy model
        public static MovableObject MakeDummy(SceneManager mgr, String name, String materialName, float size)
        {

            ManualObject dummy = mgr.CreateManualObject(name);
            dummy.Begin(materialName, RenderOperation.OperationTypes.OT_LINE_LIST);

            // octahedron wire shape 
            Vector3[] points = { 
                                   new Vector3(0,size,0), new Vector3(0,0,size),
                                   new Vector3(0,size,0), new Vector3(0,0,-size),
                                   new Vector3(0,size,0), new Vector3(-size,0,0),
                                   new Vector3(0,size,0), new Vector3(size,0,0),

                                   new Vector3(0,-size,0), new Vector3(0,0,size),
                                   new Vector3(0,-size,0), new Vector3(0,0,-size),
                                   new Vector3(0,-size,0), new Vector3(-size,0,0),
                                   new Vector3(0,-size,0), new Vector3(size,0,0),

                                   new Vector3(-size,0,0), new Vector3(0,0,size),
                                   new Vector3(0,0,size), new Vector3(size,0,0),
                                   new Vector3(size,0,0), new Vector3(0,0,-size),
                                   new Vector3(0,0,-size), new Vector3(-size,0,0),
                               };

            foreach (Vector3 v in points)
            {
                dummy.Position(v);
            }
            
            dummy.End();

            return dummy;
        }

        /* implement someday ;)
        unsafe public void CreateSphere(string strName, float r, int nRings, int nSegments)
        {
            MeshPtr pSphere = MeshManager.Singleton.CreateManual(strName, ResourceGroupManager.DEFAULT_RESOURCE_GROUP_NAME);
            SubMesh pSphereVertex = pSphere.CreateSubMesh();

            pSphere.sharedVertexData = new VertexData();
            VertexData vertexData = pSphere.sharedVertexData;

            // define the vertex format
            VertexDeclaration vertexDecl = vertexData.vertexDeclaration;
            uint currOffset = 0;
            // positions
            vertexDecl.AddElement(0, currOffset, VertexElementType.VET_FLOAT3, VertexElementSemantic.VES_POSITION);
            currOffset += VertexElement.GetTypeSize(VertexElementType.VET_FLOAT3);
            // normals
            vertexDecl.AddElement(0, currOffset, VertexElementType.VET_FLOAT3, VertexElementSemantic.VES_NORMAL);
            currOffset += VertexElement.GetTypeSize(VertexElementType.VET_FLOAT3);
            // two dimensional texture coordinates
            vertexDecl.AddElement(0, currOffset, VertexElementType.VET_FLOAT2, VertexElementSemantic.VES_TEXTURE_COORDINATES, 0);
            currOffset += VertexElement.GetTypeSize(VertexElementType.VET_FLOAT2);

            // allocate the vertex buffer
            vertexData.vertexCount = (uint)((nRings + 1) * (nSegments + 1));
            HardwareVertexBufferSharedPtr vBuf = HardwareBufferManager.Singleton.CreateVertexBuffer(vertexDecl.GetVertexSize(0), vertexData.vertexCount, HardwareBuffer.Usage.HBU_STATIC_WRITE_ONLY, false);
            VertexBufferBinding binding = vertexData.vertexBufferBinding;
            binding.SetBinding(0, vBuf);
            float* pVertex = (float*)vBuf.Lock(HardwareBuffer.LockOptions.HBL_DISCARD);

            // allocate index buffer
            pSphereVertex.indexData.indexCount = (uint)(6 * nRings * (nSegments + 1));
            pSphereVertex.indexData.indexBuffer = HardwareBufferManager.Singleton.CreateIndexBuffer(HardwareIndexBuffer.IndexType.IT_16BIT, pSphereVertex.indexData.indexCount, HardwareBuffer.Usage.HBU_STATIC_WRITE_ONLY, false);
            HardwareIndexBufferSharedPtr iBuf = pSphereVertex.indexData.indexBuffer;
            ushort* pIndices = (ushort*)iBuf.Lock(HardwareBuffer.LockOptions.HBL_DISCARD);

            float fDeltaRingAngle = (float)(Mogre.Math.PI / nRings);
            float fDeltaSegAngle = (float)(2 * Mogre.Math.PI / nSegments);
            ushort wVerticeIndex = 0;

            // Generate the group of rings for the sphere
            for (int ring = 0; ring <= nRings; ring++)
            {
                float r0 = r * Mogre.Math.Sin(ring * fDeltaRingAngle);
                float y0 = r * Mogre.Math.Cos(ring * fDeltaRingAngle);

                // Generate the group of segments for the current ring
                for (int seg = 0; seg <= nSegments; seg++)
                {
                    float x0 = r0 * Mogre.Math.Sin(seg * fDeltaSegAngle);
                    float z0 = r0 * Mogre.Math.Cos(seg * fDeltaSegAngle);

                    // Add one vertex to the strip which makes up the sphere
                    *pVertex++ = x0;
                    *pVertex++ = y0;
                    *pVertex++ = z0;

                    Vector3 vNormal = (new Vector3(x0, y0, z0)).NormalisedCopy;
                    *pVertex++ = vNormal.x;
                    *pVertex++ = vNormal.y;
                    *pVertex++ = vNormal.z;

                    *pVertex++ = (float)seg / (float)nSegments;
                    *pVertex++ = (float)ring / (float)nRings;

                    if (ring != nRings)
                    {
                        // each vertex (except the last) has six indices pointing to it
                        *pIndices++ = (ushort)(wVerticeIndex + nSegments + 1);
                        *pIndices++ = wVerticeIndex;
                        *pIndices++ = (ushort)(wVerticeIndex + nSegments);
                        *pIndices++ = (ushort)(wVerticeIndex + nSegments + 1);
                        *pIndices++ = (ushort)(wVerticeIndex + 1);
                        *pIndices++ = wVerticeIndex;
                        wVerticeIndex++;
                    }
                }; // end for seg
            } // end for ring

            // Unlock
            vBuf.Unlock();
            iBuf.Unlock();

            // Generate face list
            pSphereVertex.useSharedVertices = true;

            // the original code was missing this line:
            pSphere._setBounds(new AxisAlignedBox(new Vector3(-r, -r, -r), new Vector3(r, r, r)), false);
            pSphere._setBoundingSphereRadius(r);

            // this line makes clear the mesh is loaded (avoids memory leaks)
            pSphere.Load();
        }*/
    }
}
