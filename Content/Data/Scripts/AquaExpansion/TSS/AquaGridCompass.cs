using AquaExpansion.Core;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace AquaExpansion.AquaExpansion.TSS
{
    [MyTextSurfaceScript("AquaCompass", "Aqua Compass")]
    class AquaGridCompass : MyTSSCommon
    {
        public static float ASPECT_RATIO = 3f;
        public static float DECORATION_RATIO = 0.25f;
        public static float TEXT_RATIO = 0.25f;
        private Vector2 innerSize;
        private Vector2 linesSize;
        private StringBuilder stringBuilder = new StringBuilder();

        public override ScriptUpdate NeedsUpdate
        {
            get { return ScriptUpdate.Update10; }
        }

        public AquaGridCompass(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            innerSize = new Vector2(ASPECT_RATIO, 1.1f);
            linesSize = new Vector2(0.5f, 0.5f);
            FitRect(surface.SurfaceSize, ref innerSize);
        }

        public override void Run()
        {
            base.Run();
            using (var frame = m_surface.DrawFrame())
            {
                AddBackground(frame, new Color(m_backgroundColor, .66f));
                if (m_block == null)
                    return;
                stringBuilder.Clear();
                stringBuilder.Append($"{AquaExpansionSession.Insance.UpdateGridCompass((IMyCubeBlock)m_block)}");
                var Text = new MySprite()
                {
                    Position = new Vector2(m_halfSize.X, m_halfSize.Y - 16),
                    Size = new Vector2(innerSize.X, innerSize.Y),
                    Type = SpriteType.TEXT,
                    FontId = m_fontId,
                    Alignment = TextAlignment.CENTER,
                    Color = m_foregroundColor,
                    RotationOrScale = m_fontScale,
                    Data = stringBuilder.ToString(),
                };
                frame.Add(Text);
                stringBuilder.Clear();
                stringBuilder.Append($"{LineAnimationManager.GetFrame("Rdirection")}{LineAnimationManager.GetFrame("Ldirection")}");
                var Text2 = new MySprite()
                {
                    Position = new Vector2(m_halfSize.X, m_halfSize.Y + 16),
                    Size = new Vector2(linesSize.X, linesSize.Y),
                    Type = SpriteType.TEXT,
                    FontId = m_fontId,
                    Alignment = TextAlignment.CENTER,
                    Color = m_foregroundColor,
                    RotationOrScale = 0.5f,
                    Data = stringBuilder.ToString(),

                };
                frame.Add(Text2);

                AddBrackets(frame, new Vector2(64, 256), innerSize.Y / 256 * 0.9f, (m_size.X - innerSize.X) / 2);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
