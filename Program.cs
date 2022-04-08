using System.Diagnostics;
using Raylib_cs;
using static Raylib_cs.Raymath;
using System.Numerics;
using System;
using static Raylib_cs.Raylib;
using static Raylib_cs.KeyboardKey;
using static Raylib_cs.Color;
using static Raylib_cs.CameraProjection;
using static Raylib_cs.CameraMode;
using System.Windows.Input;

//dotnet add package -s C:\nuget-packages\ raylib-cs to add more packages

namespace SystemGenerator
{
    public class Body
    {
        public Body(float radius, Vector2 anchor)
        {
            this.orbit = null;
            this.anchor = anchor;
            this.radius = radius;
        }
        public Body(float radius, Body[] parents, float startT, float orbitRadius, float orbitPeriod)
        {
            this.orbit = new Orbit(parents, startT, orbitRadius, orbitPeriod);
            this.anchor = new Vector2(float.NaN);
            this.radius = radius;
            this.color = new Color((parents.Count()*50)+50, 0, 0, 255);
        }

        public class Orbit
        {
            public Orbit(Body[] parents, float startT, float radius, float period)
            {
                this.parents = parents;
                this.startT = startT;
                this.radius = radius;
                this.period = period;
            }
            static float t;
            public Body[] parents;
            float startT;
            float radius;
            float period;

            public Vector2 Barycenter
            {
                get
                {
                    Vector2 center = new Vector2(0);
                    if (parents.Count() != 0)
                    {
                        int counter = 0;
                        foreach (Body b in parents)
                        {
                            center += b.Pos;
                            counter++;
                        }
                        center /= counter;
                    }
                    return center;
                }
            }
            public static float Time
            {
                get { return t; }
                set { t = value; }
            }
            public float T
            {
                get { return t + startT; }
            }
            public Vector2 Pos
            {
                get
                {
                    const float pi2 = MathF.PI * 2;
                    return new Vector2(
                        MathF.Sin(pi2 * T / period) * radius,
                        MathF.Cos(pi2 * T / period) * radius
                    ) + Barycenter;
                }
            }
        }
        public Orbit orbit;
        Vector2 anchor;
        public float radius;
        public Color color;

        public Orbit OrbitData
        {
            get { return orbit; }
        }

        public float Radius
        {
            get { return radius; }
        }

        public Vector2 Pos
        {
            get
            {
            return orbit?.Pos ?? anchor;
            }
        }
    }
    class UISlider
    {
        public Vector2 position;
        public int sliderPosition;

        public void Render()
        {
            DrawRectangle((int)position.X,(int)position.Y,100,25,WHITE);
            DrawRectangle((int)position.X+2,(int)position.Y+2,96,21,BLACK);
            DrawRectangle(sliderPosition+(int)position.X+4,(int)position.Y+4,9,17,WHITE);
        }

        public void Simulate()
        {
            if (GetMousePosition().X > sliderPosition && GetMousePosition().X < sliderPosition + (28+9) && GetMousePosition().Y > position.Y+2 && GetMousePosition().Y < position.Y+26 && IsMouseButtonDown(0))
            {
                sliderPosition = (int)Math.Min(Math.Max(GetMousePosition().X-21,0),83);
            }
        }
    }
    static class SysGenProgram
    {
        private static Random rnd = new Random();
        public static float RandomFloat(float min, float max)
        {
            return min + (max - min) * (((float)rnd.Next(Int32.MaxValue)) / (float)Int32.MaxValue);
        }
        public static Vector2 RandomPoint(Vector2 origin, float radius)
        {
            Vector2 pt = new Vector2(RandomFloat(-1, 1), RandomFloat(-1, 1));
            return Vector2.Normalize(pt) * RandomFloat(0, radius);
        }
        public static void Main()
        {
            //------------INITIALIZATION PHASE------------
            //window and camera
            const int screenWidth = 800;
            const int screenHeight = 450;
            InitWindow(screenWidth,screenHeight, "Ditzy's System Generator");
            HideCursor();
            Camera3D camera = new Camera3D(new Vector3(0,10,10),new Vector3(0,0,0),new Vector3(0f,1f,0f),90f,CAMERA_PERSPECTIVE);

            //modifiers
            float timeScale = 1f;
            float orbitScale = 1f;

            //create UI features
            UISlider timescaleSlider = new UISlider();
            timescaleSlider.sliderPosition = 16;
            timescaleSlider.position = new Vector2(10,10);

            UISlider orbitSlider = new UISlider();
            orbitSlider.sliderPosition = 83;
            orbitSlider.position = new Vector2(10,40);

            //------------GENERATION PHASE------------
            List<Body> system = new List<Body>(0);
            int anchors = 1 + rnd.Next(2);
            int satellites = rnd.Next(32);
            system.Capacity = anchors + satellites;
            int i = 0;
            for (; i < anchors; ++i)
            {
               system.Add(new Body(RandomFloat(5, 15), RandomPoint(new Vector2(0), 30)));
            }
            for (; i < anchors + satellites; ++i)
            {
                List<Body> subset = new List<Body>();
                if (rnd.Next(2) == 1)
                {
                    int parentCount = rnd.Next(Math.Min(2, system.Count)); // maximum parents for a single body
                    subset = new List<Body>(system);
                    while (subset.Count > parentCount)
                    {
                        subset.RemoveAt(rnd.Next(subset.Count - 1));
                    }
                }
                float orbitSize = RandomFloat(100,2000);
                if(subset.Count() > 0)
                {
                    system.Add(new Body(subset[0].radius/rnd.Next(3,7), subset.ToArray(), RandomFloat(0, 1), orbitSize/8, orbitSize/8));
                }
                else
                {
                    system.Add(new Body(RandomFloat(1, 4), subset.ToArray(), RandomFloat(0, 1), orbitSize, orbitSize));
                }
            }

            // Display all planets
            int counter = 0;
            foreach (Body b in system)
            {
                Console.Write("Body {0}, ", counter++);
                if (counter <= anchors)
                    Console.WriteLine("Anchor:");
                else
                    Console.WriteLine("Satellite:");
                if (b.OrbitData != null)
                    Console.WriteLine(" Orbiting {0}", b.OrbitData.Barycenter);
                Console.WriteLine(" Position {0}", b.Pos);
                Console.WriteLine(" Radius {0}", b.Radius);
            }

            //main loop
            SetCameraMode(camera, CAMERA_FREE);
            SetTargetFPS(60);
            while (!WindowShouldClose())
            {
                //------------SIMULATION PHASE------------
                UpdateCamera(ref camera);
                timescaleSlider.Simulate();
                timeScale = (float)Math.Pow(timescaleSlider.sliderPosition,1.01f);
                orbitSlider.Simulate();
                orbitScale = (int)((1000/83)*orbitSlider.sliderPosition);

                //move planets
                counter = 0;
                foreach (Body b in system)
                {
                    Body.Orbit.Time += GetFrameTime()*0.1f*timeScale;
                }

                //------------RENDERING PHASE------------
                BeginDrawing();
                BeginMode3D(camera);
                ClearBackground(BLACK);

                //render debug info (such as parent positions) THIS ISN'T FINISHED AS OF UPLOADING TO THE REPOSITORY! 
                counter = 0;
                foreach (Body b in system)
                {
                    Vector2 averagePos = new Vector2(0,0);
                    float orbitMult = orbitSlider.sliderPosition/83f;
                    if (b.orbit.parents.Count() > 0)
                    {
                        int counterParents = 0;
                        foreach (Body a in b.orbit.parents)
                        {
                            counterParents++;
                            averagePos += b.orbit.parents[0].Pos;
                        }
                        averagePos /= b.orbit.parents.Count();
                    }
                    DrawSphere(new Vector3(averagePos.X*orbitMult,averagePos.Y*orbitMult,0), 5, BLUE);
                }

                //render planets
                counter = 0;
                foreach (Body b in system)
                {
                    
                    float orbitMult = orbitSlider.sliderPosition/83f;
                    DrawSphere(new Vector3(b.Pos.X*orbitMult,b.Pos.Y*orbitMult,0), b.Radius, b.color);
                }

                EndMode3D();

                //render sliders
                timescaleSlider.Render();
                orbitSlider.Render();

                //mouse pointer
                DrawCircleV(GetMousePosition(), 1, WHITE);
                EndDrawing();
            }
            CloseWindow();
        }
    }
}