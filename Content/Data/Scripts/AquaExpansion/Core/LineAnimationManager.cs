using System;
using System.Collections.Generic;
using System.Text;

namespace AquaExpansion.Core
{
    /// <summary>
    /// Manages multiple ASCII/text-based animations.
    /// Allows registering animations by name and updating them all in a centralized way.
    /// </summary>
    /// <author>YOYOMAN_MODDER</author>
    public static class LineAnimationManager
    {
        /// <summary>
        /// Manages multiple ASCII/text-based animations.
        /// Supports both frame-based (Animation) and procedural vertical (VerticalAnimation) types.
        /// </summary>
        /// <author>YOYOMAN_MODDER</author> V2
        /// 
        private static readonly Dictionary<string, IAnimation> animations = new Dictionary<string, IAnimation>();
        /// <summary>
        /// Initializes all animations. Call once at startup.
        /// </summary>
        /// 
        public static void Init(int Rate)
        {
            // ----- Frame-based animations -----
            animations["Rdirection"] = new Animation(new[]
           {
                "      ",
                "     <",
                "    <<",
                "   <<<",
                "  <<<<",
                " <<<<<",
                "<<<<<<"
            }, Rate);

            animations["Ldirection"] = new Animation(new[]
          {
                "      ",
                ">     ",
                ">>    ",
                ">>>   ",
                ">>>>  ",
                ">>>>> ",
                ">>>>>>"
            }, Rate);

            /*animations["gear"] = new Animation(new[]
            {
                "(o o)", "(o-O)", "(o o)", "(O-o)"
            }, Rate);*/

            /*animations["wave"] = new Animation(new[]
            {
                " ~     ",
                "  ~    ",
                "   ~   ",
                "    ~  ",
                "     ~ ",
            }, Rate);*/

            /*animations["progress"] = new Animation(new[]
            {
                "[      ]",
                "[=     ]",
                "[==    ]",
                "[===   ]",
                "[====  ]",
                "[===== ]",
                "[======]"
            }, Rate);*/

            /*animations["money"] = new Animation(new[]
            {
                "[      ]",
                "[$     ]",
                "[$$    ]",
                "[$$$   ]",
                "[$$$$  ]",
                "[$$$$$ ]",
                "[$$$$$$]"
            }, Rate);

            animations["fail"] = new Animation(new[]
            {
                "[      ]",
                "[X     ]",
                "[XX    ]",
                "[XXX   ]",
                "[XXXX  ]",
                "[XXXXX ]",
                "[XXXXXX]"
            }, Rate);

            animations["Lprogress"] = new Animation(new[]
            {
                " ",
                "= ",
                " ="

            }, Rate);

            animations["grinder_H"] = new Animation(new[]
            {
                @"--*O*--",
                @"-/*-O-*\-",
                @"--*O*--",
                @"-\*-O-*-/",
            }, Rate);

            animations["arrowTop"] = new Animation(new[]
            {
                "   ^   ",
                "   |   ",
                "   |   ",
                "   ^   ",
            }, Rate);*/

            // ----- Procedural vertical animation -----
            animations["bouncer"] = new VerticalAnimation(8, "█", 7);
        }

        /// <summary>
        /// Updates all registered animations. Call once per game tick.
        /// </summary>
        /// 
        public static void Update()
        {
            foreach (var anim in animations.Values)
                anim.Update();
        }

        /// <summary>
        /// Returns the current frame of the animation by key.
        /// </summary>
        /// 
        public static string GetFrame(string key)
        {
            IAnimation anim;
            return animations.TryGetValue(key, out anim) ? anim.CurrentFrame : "[ANIM?]";
        }
    }

    /// <summary>
    /// Common interface for all animation types.
    /// </summary>
    /// 
    public interface IAnimation
    {
        void Update();
        string CurrentFrame { get; }
        void Reset();
    }

    /// <summary>
    /// Frame-based animation (your original Animation class).
    /// </summary>
    /// 
    public class Animation : IAnimation
    {
        private readonly string[] frames;
        private readonly int ticksPerFrame;
        private int tick;
        private int index;

        public Animation(string[] frames, int ticksPerFrame = 7)
        {
            this.frames = frames ?? new[] { "[EMPTY]" };
            this.ticksPerFrame = Math.Max(1, ticksPerFrame);
            tick = 0;
            index = 0;
        }

        public void Update()
        {
            tick++;
            if (tick >= ticksPerFrame)
            {
                tick = 0;
                index = (index + 1) % frames.Length;
            }
        }

        public string CurrentFrame
        {
            get { return frames[index]; }
        }

        public void Reset()
        {
            tick = 0;
            index = 0;
        }
    }

    /// <summary>
    /// Procedural vertical bouncing animation.
    /// </summary>
    /// 
    public class VerticalAnimation : IAnimation
    {
        private readonly int rows;
        private readonly string marker;
        private readonly int ticksPerStep;
        private int tick;
        private int pos;
        private int dir = 1;

        public VerticalAnimation(int rows = 12, string marker = "█", int ticksPerStep = 6)
        {
            this.rows = Math.Max(3, rows);
            this.marker = marker;
            this.ticksPerStep = Math.Max(1, ticksPerStep);
            tick = 0;
            pos = 0;
        }

        public void Update()
        {
            tick++;
            if (tick >= ticksPerStep)
            {
                tick = 0;
                pos += dir;
                if (pos >= rows - 1) { pos = rows - 1; dir = -1; }
                else if (pos <= 0) { pos = 0; dir = 1; }
            }
        }

        public string CurrentFrame
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine("+ +");
                for (int i = 0; i < rows; i++)
                {
                    string mid = (i == pos) ? marker : " ";
                    sb.Append("|").Append(mid).Append("|").AppendLine();
                }
                sb.AppendLine("+ +");
                return sb.ToString();
            }
        }

        public void Reset()
        {
            tick = 0;
            pos = 0;
            dir = 1;
        }
    }
}
