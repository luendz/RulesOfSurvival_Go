using UnityEngine;

namespace ROS.Game.World
{
    /// <summary>
    /// Construye casas greybox con Unity Primitives: paredes, piso, techo
    /// y hueco de puerta funcional. Sin dependencias de arte externo.
    /// </summary>
    public static class GreyboxBuilding
    {
        // ----------------------------------------------------------------
        // Presets públicos

        public static GameObject CreateSmall(Vector3 worldPos, float yRotation = 0f)
        {
            var cfg = new Config
            {
                Width     = 8f,
                Depth     = 7f,
                Height    = 3.2f,
                WallT     = 0.28f,
                DoorW     = 1.2f,
                DoorH     = 2.2f,
                FloorColor = new Color(0.38f, 0.32f, 0.26f),
                WallColor  = new Color(0.78f, 0.72f, 0.64f),
            };
            return Build("SmallHouse", cfg, worldPos, yRotation);
        }

        public static GameObject CreateLarge(Vector3 worldPos, float yRotation = 0f)
        {
            var cfg = new Config
            {
                Width     = 14f,
                Depth     = 10f,
                Height    = 4f,
                WallT     = 0.30f,
                DoorW     = 1.3f,
                DoorH     = 2.4f,
                FloorColor = new Color(0.35f, 0.30f, 0.24f),
                WallColor  = new Color(0.72f, 0.66f, 0.58f),
                HasInternalWall = true,
            };
            return Build("LargeHouse", cfg, worldPos, yRotation);
        }

        // ----------------------------------------------------------------

        private struct Config
        {
            public float Width, Depth, Height, WallT;
            public float DoorW, DoorH;
            public Color FloorColor, WallColor;
            public bool  HasInternalWall;
        }

        private static GameObject Build(
            string name, Config c, Vector3 worldPos, float yRot)
        {
            var root = new GameObject(name);
            root.transform.position    = worldPos;
            root.transform.eulerAngles = new Vector3(0f, yRot, 0f);

            Material wallMat  = MakeMat(c.WallColor);
            Material floorMat = MakeMat(c.FloorColor);

            float hw = c.Width  * 0.5f;
            float hd = c.Depth  * 0.5f;
            float hh = c.Height * 0.5f;

            // Piso
            MakeCube(root, "Floor",
                new Vector3(0f, -c.WallT * 0.5f, 0f),
                new Vector3(c.Width, c.WallT, c.Depth),
                floorMat);

            // Techo
            MakeCube(root, "Ceiling",
                new Vector3(0f, c.Height + c.WallT * 0.5f, 0f),
                new Vector3(c.Width, c.WallT, c.Depth),
                wallMat);

            // Pared trasera (Z+)
            MakeCube(root, "WallBack",
                new Vector3(0f, hh, hd + c.WallT * 0.5f),
                new Vector3(c.Width + c.WallT * 2f, c.Height, c.WallT),
                wallMat);

            // Pared izquierda (X-)
            MakeCube(root, "WallLeft",
                new Vector3(-hw - c.WallT * 0.5f, hh, 0f),
                new Vector3(c.WallT, c.Height, c.Depth),
                wallMat);

            // Pared derecha (X+)
            MakeCube(root, "WallRight",
                new Vector3(hw + c.WallT * 0.5f, hh, 0f),
                new Vector3(c.WallT, c.Height, c.Depth),
                wallMat);

            // Pared frontal (Z-): segmento izquierdo + derecho + dintel
            float sideW   = (c.Width - c.DoorW) * 0.5f;
            float fronZ   = -hd - c.WallT * 0.5f;

            MakeCube(root, "WallFrontL",
                new Vector3(-hw + sideW * 0.5f, hh, fronZ),
                new Vector3(sideW, c.Height, c.WallT),
                wallMat);

            MakeCube(root, "WallFrontR",
                new Vector3(hw - sideW * 0.5f, hh, fronZ),
                new Vector3(sideW, c.Height, c.WallT),
                wallMat);

            float lintelH = c.Height - c.DoorH;
            if (lintelH > 0.01f)
            {
                MakeCube(root, "WallFrontLintel",
                    new Vector3(0f, c.DoorH + lintelH * 0.5f, fronZ),
                    new Vector3(c.DoorW, lintelH, c.WallT),
                    wallMat);
            }

            // Puerta frontal — bisagra en extremo izquierdo del hueco
            DoorController.Create(
                root.transform,
                localHingePos: new Vector3(-c.DoorW * 0.5f, c.DoorH * 0.5f, fronZ),
                width:     c.DoorW,
                height:    c.DoorH,
                thickness: c.WallT,
                mat:       MakeMat(new Color(0.52f, 0.38f, 0.26f)));

            // Pared interna central (solo casa grande)
            if (c.HasInternalWall)
            {
                float iDoorW = c.DoorW;
                float iSideW = (c.Width - iDoorW) * 0.5f;

                MakeCube(root, "WallIntL",
                    new Vector3(-hw + iSideW * 0.5f, hh, 0f),
                    new Vector3(iSideW, c.Height, c.WallT),
                    wallMat);

                MakeCube(root, "WallIntR",
                    new Vector3(hw - iSideW * 0.5f, hh, 0f),
                    new Vector3(iSideW, c.Height, c.WallT),
                    wallMat);

                if (lintelH > 0.01f)
                {
                    MakeCube(root, "WallIntLintel",
                        new Vector3(0f, c.DoorH + lintelH * 0.5f, 0f),
                        new Vector3(iDoorW, lintelH, c.WallT),
                        wallMat);
                }

                DoorController.Create(
                    root.transform,
                    localHingePos: new Vector3(-iDoorW * 0.5f, c.DoorH * 0.5f, 0f),
                    width:     iDoorW,
                    height:    c.DoorH,
                    thickness: c.WallT,
                    mat:       MakeMat(new Color(0.52f, 0.38f, 0.26f)));
            }

            return root;
        }

        // ----------------------------------------------------------------

        private static GameObject MakeCube(
            GameObject parent, string label,
            Vector3 localPos, Vector3 scale,
            Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = scale;
            go.isStatic                = true;
            if (mat != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static Material MakeMat(Color color)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard");
            var mat   = new Material(shader);
            mat.color = color;
            return mat;
        }
    }
}
