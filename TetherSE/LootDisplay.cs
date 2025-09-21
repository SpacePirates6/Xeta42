using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Gui; 
using VRage.Utils;
using VRageMath;
using Sandbox.Graphics; 

namespace TetherSE
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class LootDisplay : MySessionComponentBase
    {
        private class LogMessage
        {
            public string Message { get; set; }
            public Color Color { get; set; }
            public int Age { get; set; } // New property to track message age

            public LogMessage(string message, Color color)
            {
                Message = message;
                Color = color;
                Age = 0; // New messages start with age 0
            }
        }

        private static List<LogMessage> messages = new List<LogMessage>();
        private const int MAX_MESSAGES = 10;
        private const int FADE_THRESHOLD = 5; // Messages will fade out over 5 new messages
        private const float TEXT_SCALE = 0.8f;
        
        // Scrolling animation fields
        private static float _currentScrollOffset = 0f;
        private static float _targetScrollOffset = 0f;
        private const float SCROLL_SPEED = 0.005f; // Normalized units per update
        private static bool _isScrolling = false;

        // Start position as a percentage of the screen (normalized coordinates)
        private static Vector2 startPositionNormalized = new Vector2(0.01f, 0.1f); 
        
        // Line height as a percentage of the screen height
        private const float LINE_HEIGHT_NORMALIZED = 0.025f; 

        public static void AddMessage(string message, Color color)
        {
            // Increment age of existing messages
            foreach (var msg in messages)
            {
                msg.Age++;
            }

            messages.Add(new LogMessage(message, color));
            
            // Remove messages that are too old (fully faded)
            messages.RemoveAll(msg => msg.Age >= FADE_THRESHOLD);

            // Ensure we don't exceed MAX_MESSAGES visible at full opacity + FADE_THRESHOLD for fading messages
            while (messages.Count > MAX_MESSAGES + FADE_THRESHOLD)
            {
                messages.RemoveAt(0);
            }

            // Trigger scrolling animation
            _targetScrollOffset -= LINE_HEIGHT_NORMALIZED; // Move up by one line height
            _isScrolling = true;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (_isScrolling)
            {
                // Move current scroll offset towards target scroll offset
                if (_currentScrollOffset > _targetScrollOffset)
                {
                    _currentScrollOffset -= SCROLL_SPEED;
                    if (_currentScrollOffset <= _targetScrollOffset)
                    {
                        _currentScrollOffset = _targetScrollOffset;
                        _isScrolling = false;
                    }
                }
                else if (_currentScrollOffset < _targetScrollOffset)
                {
                    _currentScrollOffset += SCROLL_SPEED;
                    if (_currentScrollOffset >= _targetScrollOffset)
                    {
                        _currentScrollOffset = _targetScrollOffset;
                        _isScrolling = false;
                    }
                }
            }
        }

        public override void Draw()
        {
            base.Draw();

            if (messages.Count == 0) 
                return;

            try
            {
                // The current position starts at our normalized offset, adjusted by current scroll offset
                Vector2 currentPosition = startPositionNormalized + new Vector2(0, _currentScrollOffset);

                for (int i = 0; i < messages.Count; i++)
                {
                    LogMessage logMsg = messages[i];

                    // Calculate alpha based on age
                    float alpha = 1f - ((float)logMsg.Age / FADE_THRESHOLD);
                    if (alpha < 0) alpha = 0; // Ensure alpha doesn't go below 0
                    if (alpha > 1) alpha = 1; // Ensure alpha doesn't go above 1

                    Color fadedColor = new Color(logMsg.Color.R, logMsg.Color.G, logMsg.Color.B, (byte)(logMsg.Color.A * alpha));

                    MyGuiManager.DrawString(
                        font: MyFontEnum.White,
                        text: logMsg.Message,
                        normalizedCoord: currentPosition,
                        scale: TEXT_SCALE,
                        colorMask: fadedColor,
                        drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP 
                    );
                    
                    // Move down for the next line using the normalized height
                    currentPosition.Y += LINE_HEIGHT_NORMALIZED; 
                }
            }
            catch (Exception)
            {
                // Catch potential rare exceptions during drawing
            }
        }

        protected override void UnloadData()
        {
            messages.Clear();
            base.UnloadData();
        }
    }
}