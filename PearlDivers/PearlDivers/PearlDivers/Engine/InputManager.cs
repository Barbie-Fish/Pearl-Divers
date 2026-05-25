using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PearlDivers.Engine
{
    public class InputManager
    {
        private readonly HashSet<Key> keys = new HashSet<Key>();

        public void RegisterKey(Key key, bool isDown)
        {
            if (isDown) keys.Add(key);
            else keys.Remove(key);
        }

        public bool IsPressed(Key key) => keys.Contains(key);

        public (float dx, float dy) GetMovementP1()
        {
            float dx = 0, dy = 0;
            if (IsPressed(Key.W)) dy = -1;
            if (IsPressed(Key.S)) dy = 1;
            if (IsPressed(Key.A)) dx = -1;
            if (IsPressed(Key.D)) dx = 1;
            return (dx, dy);
        }

        public (float dx, float dy) GetMovementP2()
        {
            float dx = 0, dy = 0;
            if (IsPressed(Key.Up)) dy = -1;
            if (IsPressed(Key.Down)) dy = 1;
            if (IsPressed(Key.Left)) dx = -1;
            if (IsPressed(Key.Right)) dx = 1;
            return (dx, dy);
        }
    }
}
