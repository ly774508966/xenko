using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Input;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Physics;

namespace GravitySensor
{
    /// <summary>
    /// This script will handle keyboard inputs and set the scene gravity according to those inputs
    /// </summary>
    public class GravityScript : SyncScript
    {
        private Simulation simulation;

        public override void Start()
        {
            simulation = this.GetSimulation();
            simulation.Gravity = new Vector3(0, 0, 0);

            if (Input.Gravity.IsSupported) // enables the orientation sensor.
                Input.Gravity.IsEnabled = true;
        }

        public override void Update()
        {
            // no keys down and default gravity
            var gravity = new Vector3(0, 0, 0);

            // Get the gravity vector from the sensor
            if (Input.Gravity.IsEnabled)
            {
                var originalVector = Input.Gravity.Vector;
                gravity = new Vector3(originalVector.Z, originalVector.X, -originalVector.Y); // this rotation includes: (1) rotation of the scene (up = Z axis), (2) rotation of the display (Landscape)
            }

            if (Input.IsKeyDown(Keys.Up))
            {
                gravity += new Vector3(0, 10, 0.0f);
            }
            if (Input.IsKeyDown(Keys.Left))
            {
                gravity += new Vector3(-10, 0, 0.0f);
            }
            if (Input.IsKeyDown(Keys.Down))
            {
                gravity += new Vector3(0, -10, 0.0f);
            }
            if (Input.IsKeyDown(Keys.Right))
            {
                gravity += new Vector3(10, 0, 0.0f);
            }

            simulation.Gravity = gravity;
        }
    }
}