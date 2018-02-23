using System;
using Lego.Ev3.Core;
using Lego.Ev3.Desktop;
using XInput.Wrapper;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ConsoleApplication1
{
    class Program
    {
        public static NetworkCommunication netCom;
        public static UsbCommunication usbCom;
        public static Brick brick;
        public static X.Gamepad gamepad = X.Gamepad_1;
        public static bool IsConnected = false;
        public static int timerCounter = 0;
        public static int workMode = 0;
        public static int LastSpeedL = 0;
        public static int LastSpeed = 0;
        public static int LastSpeedTrigger = 0;

        public static string sound1path = "../";
        public static string sound2path = "../";
		public static string brickIp = "10.0.0.111";

        public static Timer tim1 = new Timer();

        static void Main(string[] args)
        {
            tim1.Interval = 120000;
            tim1.Elapsed += Tim1_Elapsed;

            string mode = "";

            while (mode == "")
            {
                Console.WriteLine("1 - wifi, 2 - usb");
                mode = Console.ReadLine();
                switch (mode)
                    {
                        case "1":
                            break;
                        case "2":
                            break;
                        default:
                            mode = "";
                            break;
                    }
            }

            if (mode == "1")
                connectNetwork();
            else connectUsb();

            if (X.IsAvailable)
            {
                gamepad.ConnectionChanged += Gamepad_ConnectionChanged;
                gamepad.StateChanged += Gamepad_StateChanged;
                X.StartPolling(gamepad);
            }

            readCommand();
        }

        private static void Tim1_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Timer triggered: #" + timerCounter + " - " + timerCounter*2 + " min");
            timerCounter++;

            stepmotor(100);
            System.Threading.Thread.Sleep(500);
            stepmotor(-100);
            System.Threading.Thread.Sleep(500);
            releaseBrake();
        }

        ////button mode
        //private static void Gamepad_StateChanged(object sender, EventArgs e)
        //{
        //    //left motor

        //    if (gamepad.A_down && !gamepad.X_down)
        //        turnMotorAtSpeed(OutputPort.C, -50);

        //    if (!gamepad.A_down && gamepad.X_down)
        //        turnMotorAtSpeed(OutputPort.C, 50);

        //    //right motor

        //    if (gamepad.B_down && !gamepad.Y_down)
        //        turnMotorAtSpeed(OutputPort.B, -50);

        //    if (!gamepad.B_down && gamepad.Y_down)
        //        turnMotorAtSpeed(OutputPort.B, 50);

        //    //brakes

        //    if (!gamepad.B_down && !gamepad.Y_down)
        //        turnMotorAtSpeed(OutputPort.B, 0);

        //    if (!gamepad.A_down && !gamepad.X_down)
        //        turnMotorAtSpeed(OutputPort.C, 0);

        //}

        //stick mode
        private static void Gamepad_StateChanged(object sender, EventArgs e)
        {
            if (IsConnected)
            {

                int speedL = 0;
                int speedR = 0;
                int triggerR = 0;
                int triggerL = 0;
                int triggerSpeed = 0;

                //checking trigger
                triggerL = (int)Math.Round(gamepad.RTrigger / 2.55, 0, MidpointRounding.ToEven);
                triggerR = (int)Math.Round(gamepad.LTrigger / 2.55, 0, MidpointRounding.ToEven);

                if (triggerR > 0 || triggerL > 0 || LastSpeedTrigger > 0)
                {
                    if (triggerR > triggerL)
                        triggerSpeed = triggerR;
                    else triggerSpeed = -triggerL;

                    LastSpeedTrigger = Math.Abs(triggerSpeed);

                    turnMotorAtSpeed(OutputPort.A, triggerSpeed);
                }

                if (Math.Abs(gamepad.LStick.Y) - gamepad.LStick_DeadZone > 0)
                    speedL = (int)Math.Round(gamepad.LStick.Y / 327.67, 0, MidpointRounding.ToEven);

                if (Math.Abs(gamepad.RStick.Y) - gamepad.RStick_DeadZone > 0)
                {
                    speedR = (int)Math.Round(gamepad.RStick.Y / 327.67, 0, MidpointRounding.ToEven);
                }



                if (gamepad.A_down)
                    playSound(sound1path);

                if (gamepad.X_down)
                    playSound(sound2path);

                if (gamepad.B_down)
                    turnMotorAtSpeed(OutputPort.A, 0);

                turnMotorAtSpeed(OutputPort.B, speedR);
                turnMotorAtSpeed(OutputPort.C, speedL);

            }

        }

        private static void Gamepad_ConnectionChanged(object sender, EventArgs e)
        {
            if (gamepad.IsConnected)
                Console.WriteLine("Gamepad 1 connected");
            else Console.WriteLine("Gamepad 1 disconnected");
        }

        static public void readCommand()
        {
            Console.Write(">");
            string cmd = Console.ReadLine();

            string[] cmdArray = cmd.Split(" ".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);

            if (!IsConnected)
            {
                Console.Write("Brick not connected");
                return;
            }

            switch (cmdArray[0])
                {
                case "connectUsb":
                    connectNetwork();
                    break;
                case "display":
                    display(cmdArray[1]);
                    break;
                case "clear":
                    clear();
                    break;
                case "read":
                    readCommand();
                    break;
                case "update":
                    update();
                    break;
                case "stepmotor":
                    stepmotor(Int32.Parse(cmdArray[1]));
                    break;
                case "control":
                    controlmode();
                    break;
                case "play":
                    playSound(cmdArray[1]);
                    break;
                case "playtone":
                    playTone(Int32.Parse(cmdArray[1]));
                    break;
                case "led":
                    ledColor(cmdArray[1]);
                    break;
                case "work":
                    workMode = 0;
                    startTimer();
                    break;
                case "work2":
                    workMode = 1;
                    startTimer();
                    break;
                case "trigger":
                    Tim1_Elapsed(null, null);
                    break;
                case "exit":
                    exit();
                    break;
            }

            readCommand();
                
        }

        static public void startTimer()
        {
            Tim1_Elapsed(null, null);

            if (!tim1.Enabled)
                tim1.Start();
            else tim1.Stop();
        }

        static public async void connectNetwork()
        {
            netCom = new NetworkCommunication(brickIp);
            await netCom.ConnectAsync();
            brick = new Brick(netCom);
            Console.WriteLine("Connected!");
            IsConnected = true;
        }

        static public async void connectUsb()
        {
            usbCom = new UsbCommunication();
            await usbCom.ConnectAsync();
            brick = new Brick(usbCom);
            Console.WriteLine("Connected!");
            IsConnected = true;
        }

        static public void exit()
        {
            Environment.Exit(0);
        }
        
        static public async void clear()
        {
           await brick.DirectCommand.CleanUIAsync();
           await brick.DirectCommand.UpdateUIAsync();
        }

        static public async void display(string text, bool readNext = true)
        {
            await brick.DirectCommand.CleanUIAsync();
            await brick.DirectCommand.DrawTextAsync(Color.Foreground, 0, (ushort)(brick.TopLineHeight + 2), text);
            await brick.DirectCommand.UpdateUIAsync();
        }

        static public async void update()
        {
            await brick.DirectCommand.UpdateUIAsync();
        }

        static public void controlmode()
        {
            Console.WriteLine("Entered control mode - ESC to exit");

            do
            {
                
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            Console.WriteLine("Exited control mode");
        }

        static public async void stepmotor(int steps, bool brake = true)
        {
            if (steps < 0)
                await brick.DirectCommand.SetMotorPolarityAsync(OutputPort.A, Polarity.Backward);
            else await brick.DirectCommand.SetMotorPolarityAsync(OutputPort.A, Polarity.Forward);

            await brick.DirectCommand.StepMotorAtSpeedAsync(OutputPort.A, 100, (uint)steps, brake);
        }

        static public async void releaseBrake()
        {
            await brick.DirectCommand.StopMotorAsync(OutputPort.A, false);
        }

        static public async void turnMotorAtSpeed(OutputPort port, int speed)
        {
            if (speed == 0)
                await brick.DirectCommand.StopMotorAsync(port, true);
            
            else await brick.DirectCommand.TurnMotorAtSpeedAsync(port, speed);

        }

        static public async void playSound(string soundName)
        {
            await brick.DirectCommand.PlaySoundAsync(100, soundName);
        }

        static public async void playTone(int frequency)
        {
            await brick.DirectCommand.PlayToneAsync(100, (ushort)frequency, 200);
        }

        static public async void ledColor(string color)
        {
            Enum.TryParse(color, true, out dupa); 

            await brick.DirectCommand.SetLedPatternAsync(dupa);
        }
    }
}
