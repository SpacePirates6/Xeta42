using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game; // For MyFontEnum
using VRage.Utils;
using VRageMath;
using VRage.Input;
using Sandbox.Graphics; // For MyGuiManager
using System.Xml.Serialization; // Added for XML serialization
using System.IO; // Added for file operations
using System.Linq; // Added for LINQ operations

namespace TetherSE
{
    public class LootDisplaySettings
    {
        public Vector2 Position { get; set; } = new Vector2(0.7027421f, 0.15f);
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class LootDisplay : MySessionComponentBase // Added comment to force recompile
    {
        private class LogMessage
        {
            public string ItemName { get; set; }
            public float Quantity { get; set; }
            public string SourceName { get; set; }
            public string DestinationName { get; set; }
            public Color Color { get; set; }
            public int CurrentFadeTicks { get; set; }
            public int MaxFadeTicks { get; set; }

            public string Message => $"({SourceName}) ------> {ItemName} ({FormatQuantity(Quantity)}) to ({DestinationName})";

            public LogMessage(string itemName, float quantity, string sourceName, string destinationName, Color color, int maxFadeTicks)
            {
                ItemName = itemName;
                Quantity = quantity;
                SourceName = sourceName;
                DestinationName = destinationName;
                Color = color;
                MaxFadeTicks = maxFadeTicks;
                CurrentFadeTicks = maxFadeTicks;
            }

            private string FormatQuantity(float amount)
            {
                if (amount >= 1000)
                {
                    return $"{Math.Round(amount / 1000f, 2)}k";
                }
                return Math.Round(amount, 1).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private static List<LogMessage> messages = new List<LogMessage>();
        private const int MAX_MESSAGES = 10;
        private const float TEXT_SCALE = 0.8f;
        
        // Constants for fade time calculation
        private const int BASE_FADE_TICKS = 180; // 3 seconds at 60 ticks/sec
        private const int TICKS_PER_QUANTITY_UNIT = 10; // 0.1 seconds per unit of quantity
        private const int MIN_FADE_TICKS = 60; // Minimum fade time of 1 second

        // Scrolling animation fields
        private static float _currentScrollOffset = 0f;
        private static float _targetScrollOffset = 0f;
        private const float SCROLL_SPEED = 0.005f; // Normalized units per update
        private static bool _isScrolling = false;

        // Display position and size fields (normalized coordinates)
        public static Vector2 startPositionNormalized; 
        private const float LINE_HEIGHT_NORMALIZED = 0.025f; 

        // Toggling and Movement fields
        private const MyKeys TOGGLE_KEY = MyKeys.F2;
        private static bool _displayEnabled = true;
        private static bool _toggleKeyPressedLastFrame = false;

        private const string SETTINGS_FILE_NAME = "LootDisplaySettings.xml";
        private static LootDisplaySettings _settings = new LootDisplaySettings();

        /// <summary>
        /// Sets the starting position of the loot display using normalized screen coordinates.
        /// </summary>
        /// <param name="newPosition">The new top-left position (0,0 is top-left, 1,1 is bottom-right).</param>
        public static void SetStartPosition(Vector2 newPosition)
        {
            _settings.Position = newPosition;
            SaveSettings(); // Save settings immediately after changing position
        }

        private static void SaveSettings()
        {
            try
            {
                using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(SETTINGS_FILE_NAME, typeof(LootDisplay)))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(LootDisplaySettings));
                    serializer.Serialize(writer, _settings);
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("Error", $"Failed to save LootDisplay settings: {e.Message}");
            }
        }

        private static void LoadSettings()
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(SETTINGS_FILE_NAME, typeof(LootDisplay)))
                {
                    using (TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(SETTINGS_FILE_NAME, typeof(LootDisplay)))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(LootDisplaySettings));
                        _settings = (LootDisplaySettings)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("Error", $"Failed to load LootDisplay settings: {e.Message}");
                _settings = new LootDisplaySettings(); // Revert to default on error
            }
            startPositionNormalized = _settings.Position; // Apply loaded position
        }

        public static void AddMessage(string itemName, float quantity, string sourceName, string destinationName, Color color)
        {
            int maxFadeTicks = Math.Max(MIN_FADE_TICKS, BASE_FADE_TICKS + (int)(quantity * TICKS_PER_QUANTITY_UNIT));

            // Try to find an existing message for the same item and transfer direction
            LogMessage existingMessage = messages.FirstOrDefault(m => 
                m.ItemName == itemName && 
                m.SourceName == sourceName && 
                m.DestinationName == destinationName);

            if (existingMessage != null)
            {
                existingMessage.Quantity += quantity;
                existingMessage.MaxFadeTicks = Math.Max(MIN_FADE_TICKS, BASE_FADE_TICKS + (int)(existingMessage.Quantity * TICKS_PER_QUANTITY_UNIT));
                existingMessage.CurrentFadeTicks = existingMessage.MaxFadeTicks; // Reset fade
                existingMessage.Color = color; // Update color in case it changed
            }
            else
            {
                // Remove oldest messages if exceeding max
                while (messages.Count >= MAX_MESSAGES)
                {
                    messages.RemoveAt(0);
                }

                messages.Add(new LogMessage(itemName, quantity, sourceName, destinationName, color, maxFadeTicks));
            }

            // Trigger scroll animation to show the new message
            if (messages.Count > MAX_MESSAGES)
            {
                _targetScrollOffset = -(messages.Count - MAX_MESSAGES) * LINE_HEIGHT_NORMALIZED;
            }
            else
            {
                _targetScrollOffset = 0f;
            }
            _isScrolling = true;
        }

        public static void AddMessage(string message, Color color)
        {
            // Create a dummy LogMessage to utilize the existing display logic
            // This message will not be merged with other item transfer messages
            // and will fade out like other general messages.
            AddMessage(message, 0f, "", "", color);
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // --- Handle Display Toggling ---
            bool toggleKeyPressed = MyAPIGateway.Input.IsKeyPress(TOGGLE_KEY);
            if (toggleKeyPressed && !_toggleKeyPressedLastFrame)
            {
                _displayEnabled = !_displayEnabled;
            }
            _toggleKeyPressedLastFrame = toggleKeyPressed;

            if (!_displayEnabled)
            {
                return;
            }

            // --- Handle Message Fading ---
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                messages[i].CurrentFadeTicks--;
                if (messages[i].CurrentFadeTicks <= 0)
                {
                    messages.RemoveAt(i);
                }
            }

            // --- Handle Scrolling Animation ---
            if (_isScrolling)
            {
                float difference = _targetScrollOffset - _currentScrollOffset;
                if (Math.Abs(difference) < SCROLL_SPEED)
                {
                    _currentScrollOffset = _targetScrollOffset;
                    _isScrolling = false;
                }
                else
                {
                    _currentScrollOffset += Math.Sign(difference) * SCROLL_SPEED;
                }
            }
        }

        private float GetDisplayWidthNormalized()
        {
            float maxWidth = 0f;
            foreach (var msg in messages)
            {
                Vector2 stringSize = MyGuiManager.MeasureString(MyFontEnum.White, msg.Message, TEXT_SCALE);
                if (stringSize.X > maxWidth)
                {
                    maxWidth = stringSize.X;
                }
            }
            return maxWidth;
        }

        private float GetDisplayHeightNormalized()
        {
            return messages.Count * LINE_HEIGHT_NORMALIZED;
        }

        public override void Draw()
        {
            base.Draw();

            if (messages.Count == 0 || !_displayEnabled) 
                return;

            try
            {
                // Draw background
                Vector2 displaySize = new Vector2(GetDisplayWidthNormalized(), GetDisplayHeightNormalized());
                RectangleF backgroundRectF = new RectangleF(startPositionNormalized, displaySize);
                Rectangle backgroundRect = new Rectangle(
                    (int)(backgroundRectF.X * MyAPIGateway.Session.Camera.ViewportSize.X),
                    (int)(backgroundRectF.Y * MyAPIGateway.Session.Camera.ViewportSize.Y),
                    (int)(backgroundRectF.Width * MyAPIGateway.Session.Camera.ViewportSize.X),
                    (int)(backgroundRectF.Height * MyAPIGateway.Session.Camera.ViewportSize.Y)
                );
                MyGuiManager.DrawSprite("SquareSimple", backgroundRect, new Color(0, 0, 0, 178), true, false, null);

                Vector2 currentPosition = startPositionNormalized + new Vector2(0, _currentScrollOffset);

                for (int i = 0; i < messages.Count; i++)
                {
                    LogMessage logMsg = messages[i];

                    // Calculate alpha for fade-out effect
                    float alpha = (float)logMsg.CurrentFadeTicks / logMsg.MaxFadeTicks;
                    alpha = MathHelper.Clamp(alpha, 0f, 1f);

                    Color fadedColor = new Color(logMsg.Color, alpha);

                    MyGuiManager.DrawString(
                        font: MyFontEnum.Debug,
                        text: logMsg.Message,
                        normalizedCoord: currentPosition,
                        scale: TEXT_SCALE,
                        colorMask: fadedColor,
                        drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP 
                    );
                    
                    currentPosition.Y += LINE_HEIGHT_NORMALIZED; 
                }
            }
            catch (Exception)
            {
                // Catch potential rare exceptions during drawing to prevent crashes
            }
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            LoadSettings();
        }

        protected override void UnloadData()
        {
            SaveSettings(); // Save settings when mod unloads
            messages.Clear();
            _currentScrollOffset = 0f; // Reset scroll offset on unload
            _targetScrollOffset = 0f; // Reset target scroll offset on unload
            _isScrolling = false; // Stop any ongoing scrolling
            base.UnloadData();
        }

        public override void LoadData()
        {
            base.LoadData();
            LoadSettings();
            _currentScrollOffset = 0f; // Reset scroll offset on load
            _targetScrollOffset = 0f; // Reset target scroll offset on load
            _isScrolling = false; // Stop any ongoing scrolling
        }
    }
}