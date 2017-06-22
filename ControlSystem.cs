//Please uncomment the #define line below if you want to include the sample code 
// in the compiled output.
// for the sample to work, you'll have to add a reference to the SimplSharpPro.UI dll to your project.
#define IncludeSampleCode

using System;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       				// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    		// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using System.Text;
using Crestron.SimplSharp.CrestronIO;                   // For file I/O
using System.Collections.Generic;
using Crestron.SimplSharpPro.UI;   // For UI Devices. Please include the Crestron.SimplSharpPro.UI DLL as a reference to your project.

//C:\ProgramData\Crestron\SDK\SimplSharpHelperInterface.dll


namespace SIMPLSharpProgram1
{
    public class ControlSystem : CrestronControlSystem
    {
        // Define local variables ...

        public Tsw750 My750;
        public Tsw550 My550;
        public Tsr302 My302;                        // House 302 at rf id 04
        public XpanelForSmartGraphics MyXpanel;
        public ComPort MyCOMPort;
        public IROutputPort MyIRp1, MyIRp2;
        public InternalRFExGateway MyGW;
        public CTimer MyTimer;

        //for labs copypaste
        public XpanelForSmartGraphics userInterface;    // xpanel version of TSR used for testing IP ID 40
        //end for labs copypaste

        private CrestronQueue<String> RxQueue = new CrestronQueue<string>();
        private Thread RxHandler;

        public int idk = 1;     // increment used in timer for chromecast
        public bool TSRregistered = false;
        public bool TextXPregistered = false;


        /// <summary>
        /// Constructor of the Control System Class. Make sure the constructor always exists.
        /// If it doesn't exit, the code will not run on your 3-Series processor.
        /// </summary>
        public ControlSystem()
            : base()
        {

            // Set the number of threads which you want to use in your program - At this point the threads cannot be created but we should
            // define the max number of threads which we will use in the system.
            // the right number depends on your project; do not make this number unnecessarily large
            Thread.MaxNumberOfUserThreads = 20;


            // Subscribe to the controller events (System, Program, and Etherent)
            CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
            CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);
            CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(ControlSystem_ControllerEthernetEventHandler);

            // Register all devices which the program wants to use
            // Check if device supports Ethernet
            if (this.SupportsEthernet)
            {

                My750 = new Tsw750(0xD3, this);                     // Register the TSW750 on IPID 0xD3
                My550 = new Tsw550(0xD4, this);                     // Register the TSW550 on IPID 0xD4
                MyXpanel = new XpanelForSmartGraphics(0xD5, this);  // Register the Xpanel on IPID 0xD5
                userInterface = new XpanelForSmartGraphics(0x40, this); //test xpanel of tsr IPID 0x40               

                // Register a single eventhandler for all three UIs. This guarantees that they all operate 
                // the same way.
                My750.SigChange += new SigEventHandler(MySigChangeHandler);
                My550.SigChange += new SigEventHandler(MySigChangeHandler);
                MyXpanel.SigChange += new SigEventHandler(MySigChangeHandler);
                userInterface.SigChange += new SigEventHandler(MySigChangeHandler);

                userInterface.LoadSmartObjects(@"\NVRAM\NewTSRxp.sgd");
                
                                // Register the devices for usage. This should happen after the 
                // eventhandler registration, to ensure no data is missed.

                if (My750.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("My750 failed registration. Cause: {0}", My750.RegistrationFailureReason);
                if (My550.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("My550 failed registration. Cause: {0}", My550.RegistrationFailureReason);
                if (MyXpanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("MyXpanel failed registration. Cause: {0}", MyXpanel.RegistrationFailureReason);
                if (userInterface.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("userInterface didn't register, crap");
                else
                    TextXPregistered = true;


                if (this.SupportsInternalRFGateway)
                {
                    CrestronConsole.PrintLine("Supports internal RF gw");
                    MyGW = this.ControllerRFGatewayDevice;
                    CrestronConsole.PrintLine("registered gateway?");
                    My302 = new Tsr302(0x04, MyGW);

                    My302.SigChange += new SigEventHandler(MySigChangeHandler);
                    My302.LoadSmartObjects(@"\NVRAM\NewTSR.sgd");

                    if (My302.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                        ErrorLog.Error("My302 failed registration. Cause: {0}", My302.RegistrationFailureReason);
                    else
                        TSRregistered = true;
                }

                if (TSRregistered == true && TextXPregistered == true)
                    SetPage("main");
                My302.ButtonStateChange += new ButtonEventHandler(MyButtonChangeHandler);

                //#if DEBUG                
                foreach (KeyValuePair<uint, SmartObject> kvp in userInterface.SmartObjects)
                {
                    kvp.Value.SigChange += new SmartObjectSigChangeEventHandler(SmartObject_SigChange);
                    CrestronConsole.PrintLine("Smart Object ID {0} on {1}", kvp.Value.ID, kvp.Value.Device.ToString());
                    foreach (Sig sig in kvp.Value.BooleanInput)
                    {
                        CrestronConsole.PrintLine("Boolean Input Signal Name:{0}", sig.Name);
                    }
                    foreach (Sig sig in kvp.Value.BooleanOutput)
                    {
                        CrestronConsole.PrintLine("Boolean Output Signal Name:{0}", sig.Name);
                    }
                    foreach (Sig sig in kvp.Value.StringInput)
                    {
                        CrestronConsole.PrintLine("String Input Signal Name:{0}", sig.Name);
                    }
                    foreach (Sig sig in kvp.Value.StringOutput)
                    {
                        CrestronConsole.PrintLine("String Output Signal Name:{0}", sig.Name);
                    }
                    foreach (Sig sig in kvp.Value.UShortInput)
                    {
                        CrestronConsole.PrintLine("Ushort Input Signal Name:{0}", sig.Name);
                    }
                    foreach (Sig sig in kvp.Value.UShortOutput)
                    {
                        CrestronConsole.PrintLine("Ushort Outputreboot Signal Name:{0}", sig.Name);
                    }

                }
                foreach (KeyValuePair<uint, SmartObject> kvp2 in My302.SmartObjects)
                {
                    kvp2.Value.SigChange += new SmartObjectSigChangeEventHandler(SmartObject_SigChange);
                    CrestronConsole.PrintLine("Smart Object ID {0} on {1}", kvp2.Value.ID, kvp2.Value.Device.ToString());
                    foreach (Sig sig in kvp2.Value.BooleanInput)
                    {
                        CrestronConsole.PrintLine("Boolean Input Signal Name:{0}", sig.Name);
                    }
                    foreach (Sig sig in kvp2.Value.BooleanOutput)
                    {
                        CrestronConsole.PrintLine("Boolean Output Signal Name:{0}", sig.Name);
                    }
                    foreach (Sig sig in kvp2.Value.StringInput)
                    {
                        CrestronConsole.PrintLine("String Input Signal Name:{0}", sig.Name);
                    }
                    foreach (Sig sig in kvp2.Value.StringOutput)
                    {
                        CrestronConsole.PrintLine("String Output Signal Name:{0}", sig.Name);
                    }
                    foreach (Sig sig in kvp2.Value.UShortInput)
                    {
                        CrestronConsole.PrintLine("Ushort Input Signal Name:{0}", sig.Name);
                    }
                    foreach (Sig sig in kvp2.Value.UShortOutput)
                    {
                        CrestronConsole.PrintLine("Ushort Outputreboot Signal Name:{0}", sig.Name);
                    }

                }
                //#endif

            }

            if (this.SupportsComPort)
            {
                MyCOMPort = this.ComPorts[1];
                MyCOMPort.SerialDataReceived += new ComPortDataReceivedEvent(myComPort_SerialDataReceived);

                if (MyCOMPort.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("COM Port couldn't be registered. Cause: {0}", MyCOMPort.DeviceRegistrationFailureReason);

                if (MyCOMPort.Registered)
                    MyCOMPort.SetComPortSpec(
                                        ComPort.eComBaudRates.ComspecBaudRate38400,
                                        ComPort.eComDataBits.ComspecDataBits8,
                                        ComPort.eComParityType.ComspecParityNone,
                                        ComPort.eComStopBits.ComspecStopBits1,
                                        ComPort.eComProtocolType.ComspecProtocolRS232,
                                        ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                                        ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
                                         false);
            }
            if (this.SupportsIROut)
            {
                MyIRp1 = this.IROutputPorts[1];
                MyIRp2 = this.IROutputPorts[2];
                try
                {
                    MyIRp1.Register();
                    MyIRp1.LoadIRDriver(string.Format("{0}\\VIZIO_XVT3D424SV.ir", Directory.GetApplicationDirectory()));      //puts the vizio driver on IR port 1
                    CrestronConsole.PrintLine("reg irp1");
                    MyIRp2.Register();
                    MyIRp2.LoadIRDriver(string.Format("{0}\\Motorola_QIP7100_HD.ir", Directory.GetApplicationDirectory()));   //puts the cable box driver on IR port 2
                    CrestronConsole.PrintLine("reg irp2");
                }
                catch (Exception ex)
                {
                    ErrorLog.Error("IROutput Error: {0}", ex.ToString());
                }


                // Test command...
                CrestronConsole.AddNewConsoleCommand(new SimplSharpProConsoleCmdFunction(TestIR), "IRSend", "IRCommandToSend", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.PrintLine("just added command irsend");

                CrestronConsole.AddNewConsoleCommand(new SimplSharpProConsoleCmdFunction(TSRtest), "tsrtest", "send something high?", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.PrintLine("just added command tsrtest");


                //copypaste from labs
                // this worked for indirect text but I'm not using it now 
                //userInterface.SmartObjects[1].StringInput["Item 1 Text"].StringValue = "string to send";

                /*if (MyIRp1.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("IR port couldn't be registered. Cause {0}", MyIRp1.DeviceRegistrationFailureReason);
                if (MyIRp1.Registered)
                    ErrorLog.Notice("IR reg correctly");*/
                //   string IRD = "vizio_xvt_series";
                //   MyIRp1.LoadIRDriver(IRD);


            }

        }

        void TestIR(string cmdIn)
        {
            CrestronConsole.PrintLine("in void testir");
            // This will pulse the "POWER_ON" function on the built in IR Port 1
            MyIRp1.PressAndRelease("POWER_ON", 1000);   // turns on the tv
            ErrorLog.Notice("sent irsend command");
        }

        void TSRtest(string cmdIn)
        {
            CrestronConsole.PrintLine("in void tsrtest");
            My302.BooleanInput[10].BoolValue = true;
        }

        /// <summary>
        /// Overridden function... Invoked before any traffic starts flowing back and forth between the devices and the 
        /// user program. 
        /// This is used to start all the user threads and create all events / mutexes etc.
        /// This function should exit ... If this function does not exit then the program will not start
        /// </summary>
        public override void InitializeSystem()
        {
            // This should always return   
#if IncludeSampleCode
            if (this.SupportsComPort && MyCOMPort.Registered)
                RxHandler = new Thread(RxMethod, null, Thread.eThreadStartOptions.Running);
#endif
        }

#if IncludeSampleCode

        /// <summary>
        /// This method will take messages of the Receive queue, and find the 
        /// delimiter. This is where you would put the parsing.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        object RxMethod(object obj)
        {
            StringBuilder RxData = new StringBuilder();
            int Pos = -1;

            String MatchString = String.Empty;
            // the Dequeue method will wait, making this an acceptable
            // while (true) implementation.
            while (true)
            {
                try
                {
                    // removes string from queue, blocks until an item is queued
                    string tmpString = RxQueue.Dequeue();

                    if (tmpString == null)
                        return null; // terminate the thread

                    RxData.Append(tmpString); //Append received data to the COM buffer
                    MatchString = RxData.ToString();

                    //find the delimiter
                    Pos = MatchString.IndexOf(Convert.ToChar("\n"));
                    if (Pos >= 0)
                    {
                        // delimiter found
                        // create temporary string with matched data.
                        MatchString = MatchString.Substring(0, Pos + 1);
                        RxData.Remove(0, Pos + 1); // remove data from COM buffer

                        // parse data here
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Error("Exception in thread: {0}", ex.Message);
                }
            }
        }

        /// <summary>
        /// This method is an eventhandler. In this sample, it handles the signal events
        /// from the TSW750, TS550, and the XPanel.
        /// This event will not retrigger, until you exit the currently running eventhandler.
        /// Use threads, or dispatch to a worker, to exit this function quickly.
        /// </summary>
        /// <param name="currentDevice">This is the device that is calling this function. 
        /// Use it to identify, for example, which room the buttom press is associated with.</param>
        /// <param name="args">This is the signal event argument, it contains all the data you need
        /// to properly parse the event.</param>
        /// 
        // Trying this for the copypastefrom labs for the smart objects
        void SmartObject_SigChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            CrestronConsole.PrintLine("in the smart object event handler");
            switch (args.SmartObjectArgs.ID)
            {
                case 1:
                    {
                        // source voice lights brightness setup
                        switch (args.Sig.Type)
                          {
                              case eSigType.Bool:
                                  {
                                      if (args.Sig.BoolValue)
                                      {
                                          switch (args.Sig.Name)
                                          {
                                              case ("Item 1 Pressed"):
                                                  {
                                                      CrestronConsole.PrintLine("Flip source");
                                                      SetPage("sources");
                                                      break;
                                                  }
                                              case ("Item 2 Pressed"):
                                                  {
                                                      CrestronConsole.PrintLine("Flip voice");
                                                      SetPage("voice");
                                                      break;
                                                  }
                                              case ("Item 3 Pressed"):
                                                  {
                                                      CrestronConsole.PrintLine("Flip lights");
                                                      SetPage("lights");
                                                      break;
                                                  }
                                              case ("Item 4 Pressed"):
                                                  {
                                                      CrestronConsole.PrintLine("Flip brightness");
                                                      SetPage("settings");
                                                      break;
                                                  }
                                              case ("Item 5 Pressed"):
                                                  {
                                                      CrestronConsole.PrintLine("Flip Setup");
                                                      SetPage("setup");
                                                      break;
                                                  }
                                          }
                                      }
                                      break;
                                  }
                        }
                        break;   
                    }
                case 2:
                    {
                        switch (args.Sig.Type)
                        {
                            case eSigType.Bool:
                                {
                                    if (args.Sig.BoolValue)
                                    {
                                        switch (args.Sig.Name)
                                        {
                                            case ("Item 1 Pressed"):
                                                {
                                                    SetPage("sorry");
                                                    // CrestronConsole.PrintLine("Netflix -> Power on TV");     cbs
                                                    // TVControl("ON", 0);
                                                    break;
                                                }
                                            case ("Item 2 Pressed"):
                                                {
                                                    SetPage("sorry");
                                                    // CrestronConsole.PrintLine("Cable -> Power on TV");       fox
                                                    //  MyIRp1.PressAndRelease("POWER_ON", 1000);
                                                    break;
                                                }
                                            case ("Item 3 Pressed"):
                                                {
                                                    SetPage("sorry");
                                                    // CrestronConsole.PrintLine("IT IS three");                tnt
                                                    break;
                                                }
                                            case ("Item 4 Pressed"):
                                                {
                                                    SetPage("sorry");
                                                    // CrestronConsole.PrintLine("Boxee -> Power on TV");       food
                                                    // MyIRp1.PressAndRelease("POWER_ON", 1000);
                                                    break;
                                                }
                                         }
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                    
                case 3:
                    {
                        switch (args.Sig.Type)
                          {
                              case eSigType.Bool:
                                  {
                                      if (args.Sig.BoolValue)
                                      {
                                          switch (args.Sig.Name)
                                          {
                                              case ("Item 1 Pressed"):
                                                  {
                                                      CrestronConsole.PrintLine("Netflix -> Power on TV");
                                                      TVControl("ON", 0);
                                                      TVControl("Source", 3);
                                                      SetPage("chromecast");
                                                      break;
                                                  }
                                              case ("Item 2 Pressed"):
                                                  {
                                                      CrestronConsole.PrintLine("Cable -> Power on TV");
                                                      TVControl("ON", 0);
                                                      TVControl("SOURCE", 2);
                                                      SetPage("tv");
                                                      break;
                                                  }
                                              case ("Item 3 Pressed"):
                                                  {
                                                      CrestronConsole.PrintLine("IT IS three");
                                                      break;
                                                  }
                                              case ("Item 4 Pressed"):
                                                  {
                                                      CrestronConsole.PrintLine("Boxee -> Power on TV");
                                                      TVControl("ON", 0); 
                                                      break;
                                                  }
                                              case ("Item 5 Pressed"):
                                                  {
                                                      CrestronConsole.PrintLine("Chromecast -> Power on TV");
                                                      TVControl("ON", 0); 
                                                      TVControl("SOURCE", 3);
                                                      SetPage("chromecast");
                                                      break;
                                                  }
                                          }
                                      }
                                      break;
                                  }
                        }
                        break;   
                    }
            }
            //break;
        }
        void MySigChangeHandler(GenericBase currentDevice, SigEventArgs args)
        {
            switch (args.Sig.Type)
            {
                case eSigType.Bool:
                    {
                        if (args.Sig.BoolValue) // only process the press, not the release;
                        {
                            switch (args.Sig.Number)
                            {
                                case 5:
                                    {
                                        if (currentDevice == MyXpanel)
                                            ((BasicTriList)currentDevice).StringInput[10].StringValue = "#woah 5";
                                        //ErrorLog.Error("send woah?");
                                        string[] IRCmds = MyIRp1.AvailableIRCmds();
                                        int ircmdsl = IRCmds.Length;
                                        for (int i = 0; i < ircmdsl; i++)
                                        {
                                            ErrorLog.Notice(IRCmds[i]);
                                        }
                                        break;
                                    }
                                case 6:
                                    {
                                        if (currentDevice == MyXpanel)
                                            ((BasicTriList)currentDevice).StringInput[10].StringValue = "#wow 6";
                                        //ErrorLog.Error("send woah?");
                                        
                                        break;
                                    }
                                case 10:
                                    {
                                        //MyCOMPort.Send("!start\n");
                                        ErrorLog.Error("You hit 10!\n");
                                        
                                        break;
                                    }
                                case 11:                                                                                    // this is some test 
                                    {
                                        MyCOMPort.Send("!stop\n");
                                        if (currentDevice == My550 || currentDevice == My750)
                                            ((BasicTriList)currentDevice).BooleanInput[50].BoolValue = true; // send digital value to touchscreen
                                        else
                                        {
                                            MyXpanel.BooleanInput[50].BoolValue = true;   // send digital value to xpanel
                                            MyXpanel.BooleanInput[120].BoolValue = false; // send digital value to xpanel
                                        }
                                        break;
                                    }
                                case 25:
                                    {
                                        CrestronConsole.PrintLine("25");
                                        if (currentDevice == userInterface || currentDevice == My302)  
                                        { SetPage("main"); CrestronConsole.PrintLine("in for deviced 25"); }
                                        break;
                                    }
                            }
                        }
                        if (args.Sig.Type == eSigType.UShort)
                        {
                            switch (args.Sig.Number)
                            {
                                case (15):
                                    {
                                        MyCOMPort.Send(String.Format("!volume={0}\n", args.Sig.UShortValue));
                                        break;
                                    }
                            }
                        }
                        break;
                    }

            }
        }

        void MyButtonChangeHandler(GenericBase currentDevice, ButtonEventArgs args)
        { 
            switch (args.NewButtonState)
            {
                case eButtonState.Pressed:
                    {
                        switch (args.Button.Name)
                        {
                            case eButtonName.VolumeDown:
                                {
                                    TVControl("Volume Down", 0);
                                    break;
                                }
                            case eButtonName.VolumeUp:
                                {
                                    TVControl("Volume Up", 0);
                                    break;
                                }
                            case eButtonName.Mute:
                                {
                                    TVControl("Mute", 0);
                                    break;
                                }
                        }
                        break;
                    }
                case eButtonState.Released:
                    {
                        switch (args.Button.Name)
                        {
                            case eButtonName.Power:
                                {
                                    CrestronConsole.PrintLine("POWER!!!");
                                    MyIRp1.PressAndRelease("POWER_OFF", 1000);
                                    break;
                                }
                            case eButtonName.VolumeDown:
                                {
                                    TVControl("Volume Down", 1);
                                    break;
                                }
                            case eButtonName.VolumeUp:
                                {
                                    TVControl("Volume Up", 1);
                                    break;
                                }

                        }
                        break;
                    }
            }
        }

void SetPage(string page)
        {
            if (page == "main")
            {
                userInterface.BooleanInput[10].BoolValue = true;
                userInterface.BooleanInput[11].BoolValue = false;
                userInterface.BooleanInput[12].BoolValue = false;
                userInterface.BooleanInput[13].BoolValue = false;
                userInterface.BooleanInput[14].BoolValue = false;
                userInterface.BooleanInput[15].BoolValue = false;
                userInterface.BooleanInput[16].BoolValue = false;
                userInterface.BooleanInput[17].BoolValue = false;
                userInterface.BooleanInput[18].BoolValue = false;
                userInterface.BooleanInput[19].BoolValue = false;
                
                My302.BooleanInput[10].BoolValue = true;
                My302.BooleanInput[11].BoolValue = false;
                My302.BooleanInput[12].BoolValue = false;
                My302.BooleanInput[13].BoolValue = false;
                My302.BooleanInput[14].BoolValue = false;
                My302.BooleanInput[15].BoolValue = false;
                My302.BooleanInput[16].BoolValue = false;
                My302.BooleanInput[17].BoolValue = false;
                My302.BooleanInput[18].BoolValue = false;
                My302.BooleanInput[19].BoolValue = false;
                

            }
            else if (page == "sources")
            {
                userInterface.BooleanInput[10].BoolValue = false;
                userInterface.BooleanInput[11].BoolValue = true;
                userInterface.BooleanInput[12].BoolValue = false;
                userInterface.BooleanInput[13].BoolValue = false;
                userInterface.BooleanInput[14].BoolValue = false;
                userInterface.BooleanInput[15].BoolValue = false;
                userInterface.BooleanInput[16].BoolValue = false;
                userInterface.BooleanInput[17].BoolValue = false;
                userInterface.BooleanInput[18].BoolValue = false;
                userInterface.BooleanInput[19].BoolValue = false;
                
                My302.BooleanInput[10].BoolValue = false;
                My302.BooleanInput[11].BoolValue = true;
                My302.BooleanInput[12].BoolValue = false;
                My302.BooleanInput[13].BoolValue = false;
                My302.BooleanInput[14].BoolValue = false;
                My302.BooleanInput[15].BoolValue = false;
                My302.BooleanInput[16].BoolValue = false;
                My302.BooleanInput[17].BoolValue = false;
                My302.BooleanInput[18].BoolValue = false;
                My302.BooleanInput[19].BoolValue = false;
                
            }
            else if (page == "tv")
            {
                userInterface.BooleanInput[10].BoolValue = false;
                userInterface.BooleanInput[11].BoolValue = false;
                userInterface.BooleanInput[12].BoolValue = true;
                userInterface.BooleanInput[13].BoolValue = false;
                userInterface.BooleanInput[14].BoolValue = false;
                userInterface.BooleanInput[15].BoolValue = false;
                userInterface.BooleanInput[16].BoolValue = false;
                userInterface.BooleanInput[17].BoolValue = false;
                userInterface.BooleanInput[18].BoolValue = false;
                userInterface.BooleanInput[19].BoolValue = false;
                
                My302.BooleanInput[10].BoolValue = false;
                My302.BooleanInput[11].BoolValue = false;
                My302.BooleanInput[12].BoolValue = true;
                My302.BooleanInput[13].BoolValue = false;
                My302.BooleanInput[14].BoolValue = false;
                My302.BooleanInput[15].BoolValue = false;
                My302.BooleanInput[16].BoolValue = false;
                My302.BooleanInput[17].BoolValue = false;
                My302.BooleanInput[18].BoolValue = false;
                My302.BooleanInput[19].BoolValue = false;
                 
            }
            else if (page == "settings")
            {
                userInterface.BooleanInput[10].BoolValue = false;
                userInterface.BooleanInput[11].BoolValue = false;
                userInterface.BooleanInput[12].BoolValue = false;
                userInterface.BooleanInput[13].BoolValue = true;
                userInterface.BooleanInput[14].BoolValue = false;
                userInterface.BooleanInput[15].BoolValue = false;
                userInterface.BooleanInput[16].BoolValue = false;
                userInterface.BooleanInput[17].BoolValue = false;
                userInterface.BooleanInput[18].BoolValue = false;
                userInterface.BooleanInput[19].BoolValue = false;
                
                My302.BooleanInput[10].BoolValue = false;
                My302.BooleanInput[11].BoolValue = false;
                My302.BooleanInput[12].BoolValue = false;
                My302.BooleanInput[13].BoolValue = true;
                My302.BooleanInput[14].BoolValue = false;
                My302.BooleanInput[15].BoolValue = false;
                My302.BooleanInput[16].BoolValue = false;
                My302.BooleanInput[17].BoolValue = false;
                My302.BooleanInput[18].BoolValue = false;
                My302.BooleanInput[19].BoolValue = false;
                
            }
            else if (page == "music")
            {
                userInterface.BooleanInput[10].BoolValue = false;
                userInterface.BooleanInput[11].BoolValue = false;
                userInterface.BooleanInput[12].BoolValue = false;
                userInterface.BooleanInput[13].BoolValue = false;
                userInterface.BooleanInput[14].BoolValue = true;
                userInterface.BooleanInput[15].BoolValue = false;
                userInterface.BooleanInput[16].BoolValue = false;
                userInterface.BooleanInput[17].BoolValue = false;
                userInterface.BooleanInput[18].BoolValue = false;
                userInterface.BooleanInput[19].BoolValue = false;
                
                My302.BooleanInput[10].BoolValue = false;
                My302.BooleanInput[11].BoolValue = false;
                My302.BooleanInput[12].BoolValue = false;
                My302.BooleanInput[13].BoolValue = false;
                My302.BooleanInput[14].BoolValue = true;
                My302.BooleanInput[15].BoolValue = false;
                My302.BooleanInput[16].BoolValue = false;
                My302.BooleanInput[17].BoolValue = false;
                My302.BooleanInput[18].BoolValue = false;
                My302.BooleanInput[19].BoolValue = false;
                
            }
            else if (page == "voice")
            {
                userInterface.BooleanInput[10].BoolValue = false;
                userInterface.BooleanInput[11].BoolValue = false;
                userInterface.BooleanInput[12].BoolValue = false;
                userInterface.BooleanInput[13].BoolValue = false;
                userInterface.BooleanInput[14].BoolValue = false;
                userInterface.BooleanInput[15].BoolValue = false;
                userInterface.BooleanInput[16].BoolValue = false;
                userInterface.BooleanInput[17].BoolValue = false;
                userInterface.BooleanInput[18].BoolValue = true;
                userInterface.BooleanInput[19].BoolValue = false;
                
                My302.BooleanInput[10].BoolValue = false;
                My302.BooleanInput[11].BoolValue = false;
                My302.BooleanInput[12].BoolValue = false;
                My302.BooleanInput[13].BoolValue = false;
                My302.BooleanInput[14].BoolValue = false;
                My302.BooleanInput[15].BoolValue = false;
                My302.BooleanInput[16].BoolValue = false;
                My302.BooleanInput[17].BoolValue = false;
                My302.BooleanInput[18].BoolValue = true;
                My302.BooleanInput[19].BoolValue = false;
                 
            }
            else if (page == "lights")
            {
                userInterface.BooleanInput[10].BoolValue = false;
                userInterface.BooleanInput[11].BoolValue = false;
                userInterface.BooleanInput[12].BoolValue = false;
                userInterface.BooleanInput[13].BoolValue = false;
                userInterface.BooleanInput[14].BoolValue = false;
                userInterface.BooleanInput[15].BoolValue = true;
                userInterface.BooleanInput[16].BoolValue = false;
                userInterface.BooleanInput[17].BoolValue = false;
                userInterface.BooleanInput[18].BoolValue = false;
                userInterface.BooleanInput[19].BoolValue = false;
                
                My302.BooleanInput[10].BoolValue = false;
                My302.BooleanInput[11].BoolValue = false;
                My302.BooleanInput[12].BoolValue = false;
                My302.BooleanInput[13].BoolValue = false;
                My302.BooleanInput[14].BoolValue = false;
                My302.BooleanInput[15].BoolValue = true;
                My302.BooleanInput[16].BoolValue = false;
                My302.BooleanInput[17].BoolValue = false;
                My302.BooleanInput[18].BoolValue = false;
                My302.BooleanInput[19].BoolValue = false;
                

            }
            else if (page == "chromecast")
            {
                userInterface.BooleanInput[10].BoolValue = false;
                userInterface.BooleanInput[11].BoolValue = false;
                userInterface.BooleanInput[12].BoolValue = false;
                userInterface.BooleanInput[13].BoolValue = false;
                userInterface.BooleanInput[14].BoolValue = false;
                userInterface.BooleanInput[15].BoolValue = false;
                userInterface.BooleanInput[16].BoolValue = true;
                userInterface.BooleanInput[17].BoolValue = false;
                userInterface.BooleanInput[18].BoolValue = false;
                userInterface.BooleanInput[19].BoolValue = false;
                
                My302.BooleanInput[10].BoolValue = false;
                My302.BooleanInput[11].BoolValue = false;
                My302.BooleanInput[12].BoolValue = false;
                My302.BooleanInput[13].BoolValue = false;
                My302.BooleanInput[14].BoolValue = false;
                My302.BooleanInput[15].BoolValue = false;
                My302.BooleanInput[16].BoolValue = true;
                My302.BooleanInput[17].BoolValue = false;
                My302.BooleanInput[18].BoolValue = false;
                My302.BooleanInput[19].BoolValue = false;
                

            }
            else if (page == "setup")
            {
                userInterface.BooleanInput[10].BoolValue = false;
                userInterface.BooleanInput[11].BoolValue = false;
                userInterface.BooleanInput[12].BoolValue = false;
                userInterface.BooleanInput[13].BoolValue = false;
                userInterface.BooleanInput[14].BoolValue = false;
                userInterface.BooleanInput[15].BoolValue = false;
                userInterface.BooleanInput[16].BoolValue = false;
                userInterface.BooleanInput[17].BoolValue = true;
                userInterface.BooleanInput[18].BoolValue = false;
                userInterface.BooleanInput[19].BoolValue = false;
                
                My302.BooleanInput[10].BoolValue = false;
                My302.BooleanInput[11].BoolValue = false;
                My302.BooleanInput[12].BoolValue = false;
                My302.BooleanInput[13].BoolValue = false;
                My302.BooleanInput[14].BoolValue = false;
                My302.BooleanInput[15].BoolValue = false;
                My302.BooleanInput[16].BoolValue = false;
                My302.BooleanInput[17].BoolValue = true;
                My302.BooleanInput[18].BoolValue = false;
                My302.BooleanInput[19].BoolValue = false;
                
            }
            else if (page == "sorry")
            {
                userInterface.BooleanInput[10].BoolValue = false;
                userInterface.BooleanInput[11].BoolValue = false;
                userInterface.BooleanInput[12].BoolValue = false;
                userInterface.BooleanInput[13].BoolValue = false;
                userInterface.BooleanInput[14].BoolValue = false;
                userInterface.BooleanInput[15].BoolValue = false;
                userInterface.BooleanInput[16].BoolValue = false;
                userInterface.BooleanInput[17].BoolValue = false;
                userInterface.BooleanInput[18].BoolValue = false;
                userInterface.BooleanInput[19].BoolValue = true;
                
                My302.BooleanInput[10].BoolValue = false;
                My302.BooleanInput[11].BoolValue = false;
                My302.BooleanInput[12].BoolValue = false;
                My302.BooleanInput[13].BoolValue = false;
                My302.BooleanInput[14].BoolValue = false;
                My302.BooleanInput[15].BoolValue = false;
                My302.BooleanInput[16].BoolValue = false;
                My302.BooleanInput[17].BoolValue = false;
                My302.BooleanInput[18].BoolValue = false;
                My302.BooleanInput[19].BoolValue = true;
                
            }
            else
                CrestronConsole.PrintLine("NO SUCH PAGE EXISTS!!!");
        }


void nextir(int i)
{
  //  for (int i = 0; i < 5; i++)
   // {
        //TimerTest();
        if (i == 0)
            MyIRp1.PressAndRelease("HDMI_2", 1000);
        if (i == 1)
            MyIRp1.PressAndRelease("INPUT", 1000);
        if (i == 2 || i == 3)
            MyIRp1.PressAndRelease("DN_ARROW", 1000);
        if (i == 5)
            MyIRp1.PressAndRelease("ENTER/SELECT", 1000);

    //}
}

//public delegate void CTimerCallbackFunction();
public void TimerTest()
{
    CrestronConsole.PrintLine("| {0} | Timer Start", DateTime.Now);
    if (MyTimer == null)
    {
        CrestronConsole.PrintLine("MyTimer null");
        MyTimer = new CTimer(TimeIsUp, 2000);
    }
    else
    {
        //if (idk < 0)
        //{
            CrestronConsole.PrintLine("MyTimer Reset");
            //idk = idk * -1;
            MyTimer.Reset();
        //}
    }
}
// This gets called when MyTimer expires
private void TimeIsUp(object callbackObject)
{

    
    MyTimer.Dispose();
    MyTimer = null;
    //nextir(idk);
    if (idk == 1)
    {
        MyIRp1.PressAndRelease("HDMI_2", 10);
        //idk = idk * -1;
    }
    if (idk == 2)
    {
        MyIRp1.PressAndRelease("INPUT", 10);
        //idk = idk * -1;
    }
    if (idk == 3 || idk == 4)
    {
        MyIRp1.PressAndRelease("DN_ARROW", 10);
        //idk = idk * -1;
    }
    if (idk == 5)
    {
        MyIRp1.PressAndRelease("ENTER/SELECT", 10);
        //idk = idk * -1;
    }
    idk++;

    CrestronConsole.PrintLine("| {0} | Timer Done", DateTime.Now);
    if (idk < 6)
        TimerTest();
    CrestronConsole.PrintLine("idk is {0}", idk);

    if (idk == 6)
    {
        idk = 1;
    }
}


        void TVControl(string cmd, int val)
        {
            if (cmd == "ON")
            {
                MyIRp1.PressAndRelease("POWER_ON", 1000);
                CrestronConsole.PrintLine("In the TVControl");
            }
            else if (cmd == "SOURCE")
            {
                switch (val)
                {
                    case (1):
                        {
                            MyIRp1.PressAndRelease("HDMI_1", 1000);
                            CrestronConsole.PrintLine("hdmi 1");
                            break;
                        }
                    case (2):
                        {
                            MyIRp1.PressAndRelease("HDMI_2", 1000);
                            CrestronConsole.PrintLine("hdmi 2");
                            break;
                        }
                    case (3):
                        {
                            CrestronConsole.PrintLine("chromecast stuff");
                            //int step = 0;
                            TimerTest();
                            CrestronConsole.PrintLine("idk is {0}", idk);

                            /*
                            if (idk < 0)
                            {
                                TimerTest();
                                CrestronConsole.PrintLine("idk is {0}", idk);
                                TimerTest();
                                CrestronConsole.PrintLine("idk is {0}", idk);
                                TimerTest();
                                CrestronConsole.PrintLine("idk is {0}", idk);
                                TimerTest();
                                CrestronConsole.PrintLine("idk is {0}", idk);
                            }
                             */
                            //nextIrDelegate irDelegate = new nextIrDelegate(nextir);
                            //CTimer IRTimer = new CTimer(irDelegate(), 100);
                            //MyIRp1.PressAndRelease("HDMI_2", 1000);
                            // HOW TO DELAY???
                            //MyIRp1.PressAndRelease("INPUT", 1000);
                            // HOW TO DELAY???
                            //MyIRp1.PressAndRelease("DN_ARROW", 1000);
                            // HOW TO DELAY???
                            //MyIRp1.PressAndRelease("DN_ARROW", 1000);
                            // HOW TO DELAY???  http://www.crestronlabs.com/showthread.php?13563-Simpl-wait-sleep-or-CTimer&highlight=s%23+delay
                            //MyIRp1.PressAndRelease("ENTER/SELECT", 1000);
                            break;
                        }
                }
            }
            else if (cmd == "Volume Down")
            {
                if (val == 0)
                    MyIRp1.Press("VOL-");
                //                MyIRp1.PressAndRelease("VOL-", 1000);
                else if (val == 1)
                    MyIRp1.Release();
            }
            else if (cmd == "Volume Up")
            {
                // MyIRp1.PressAndRelease("VOL+", 1000);
                if (val == 0)
                    MyIRp1.Press("VOL+");
                else if (val == 1)
                    MyIRp1.Release();
            }            
            else if (cmd == "Mute")
            {
                MyIRp1.PressAndRelease("MUTE", 300);
            }
            else
                CrestronConsole.PrintLine("Not a recognized TV Control");
        }




        /// <summary>
        /// This event is triggered whenever an Ethernet event happens. 
        /// </summary>
        /// <param name="ethernetEventArgs">Holds all the data needed to properly parse</param>
        void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for. 
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }

        /// <summary>
        /// This event is triggered whenever a program event happens (such as stop, pause, resume, etc.)
        /// </summary>
        /// <param name="programEventType">These event arguments hold all the data to properly parse the event</param>
        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events

                    RxQueue.Enqueue(null); // The RxThread will terminate when it receives a null
                    break;
            }

        }

        /// <summary>
        /// This handler is triggered for system events
        /// </summary>
        /// <param name="systemEventType">The event argument needed to parse.</param>
        void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting. 
                    //Very limited time to preform clean up and save any settings to disk.
                    break;
            }

        }

        /// <summary>
        /// This event gets triggered whenever data comes in on the serial port. 
        /// This event will not retrigger, until you exit the currently running eventhandler.
        /// Use threads, or dispatch to a worker, to exit this function quickly.
        /// </summary>
        /// <param name="ReceivingComPort">This is a reference to the COM port sending the data</param>
        /// <param name="args">This holds all the data received.</param>
        void myComPort_SerialDataReceived(ComPort ReceivingComPort, ComPortSerialDataEventArgs args)
        {
            RxQueue.Enqueue(args.SerialData);
        }
        
       
#endif




    }
}
