using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BarracudaSample
{
    /// <summary>
    /// Utility to convert unity texture to Tensor
    /// </summary>
    public class TextureToTensor : System.IDisposable
    {
        public enum AspectMode
        {
            None,
            Fit,
            Fill,
        }

        public struct ResizeOptions
        {
            public int width;
            public int height;
            public float rotationDegree;
            public bool flipX;
            public bool flipY;
            public AspectMode aspectMode;
        }

        public RenderTexture resizeTexture { get; private set; }
        Material transfromMat;

        static readonly int _VertTransform = Shader.PropertyToID("_VertTransform");
        static readonly int _UVRect = Shader.PropertyToID("_UVRect");

        public TextureToTensor() { }

        public void Dispose()
        {
            TryDispose(resizeTexture);
            TryDispose(transfromMat);
        }

        public RenderTexture Resize(Texture texture, ResizeOptions options)
        {
            if (resizeTexture == null
            || resizeTexture.width != options.width
            || resizeTexture.height != options.height)
            {
                TryDispose(resizeTexture);
                resizeTexture = new RenderTexture(options.width, options.height, 0, GraphicsFormat.R16G16B16A16_SFloat);
            }
            if (transfromMat == null)
            {
                transfromMat = new Material(Shader.Find("Hidden/Barracuda/Resize"));
            }

            // Set options
            float rotation = options.rotationDegree;
            if (texture is WebCamTexture)
            {
                var webcamTex = (WebCamTexture)texture;
                rotation += webcamTex.videoRotationAngle;
                if (webcamTex.videoVerticallyMirrored)
                {
                    options.flipX = !options.flipX;
                }
            }
            Matrix4x4 trs = GetVertTransform(rotation, options.flipX, options.flipY);
            transfromMat.SetMatrix(_VertTransform, trs);
            transfromMat.SetVector(_UVRect, GetTextureST(
                (float)texture.width / (float)texture.height, // src
                (float)options.width / (float)options.height, // dst
                options.aspectMode));

            Graphics.Blit(texture, resizeTexture, transfromMat, 0);
            return resizeTexture;
        }

        static Vector4 GetTextureST(float srcAspect, float dstAspect, AspectMode mode)
        {
            switch (mode)
            {
                case AspectMode.None:
                    return new Vector4(1, 1, 0, 0);
                case AspectMode.Fit:
                    if (srcAspect > dstAspect)
                    {
                        float s = srcAspect / dstAspect;
                        return new Vector4(1, s, 0, (1 - s) / 2);
                    }
                    else
                    {
                        float s = dstAspect / srcAspect;
                        return new Vector4(s, 1, (1 - s) / 2, 0);
                    }
                case AspectMode.Fill:
                    if (srcAspect > dstAspect)
                    {
                        float s = dstAspect / srcAspect;
                        return new Vector4(s, 1, (1 - s) / 2, 0);
                    }
                    else
                    {
                        float s = srcAspect / dstAspect;
                        return new Vector4(1, s, 0, (1 - s) / 2);
                    }
            }
            throw new System.Exception("Unknown aspect mode");
        }

        public static Rect GetUVRect(float srcAspect, float dstAspect, AspectMode mode)
        {
            Vector4 texST = GetTextureST(srcAspect, dstAspect, mode);
            return new Rect(texST.z, texST.w, texST.x, texST.y);
        }

        static bool IsSameSize(Texture a, Texture b)
        {
            return a.width == b.width && a.height == b.height;
        }

        static void TryDispose(RenderTexture tex)
        {
            if (tex != null)
            {
                tex.Release();
                Object.Destroy(tex);
            }
        }

        static void TryDispose(Material mat)
        {
            if (mat == null)
            {
                Object.Destroy(mat);
            }
        }

        static readonly Matrix4x4 PUSH_MATRIX = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0));
        static readonly Matrix4x4 POP_MATRIX = Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));
        static Matrix4x4 GetVertTransform(float rotation, bool invertX, bool invertY)
        {
            Vector3 scale = new Vector3(
                invertX ? -1 : 1,
                invertY ? -1 : 1,
                1);
            Matrix4x4 trs = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.Euler(0, 0, rotation),
                scale
            );
            return PUSH_MATRIX * trs * POP_MATRIX;
        }
    }
}
