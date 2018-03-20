﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using ABB.Robotics;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.RapidDomain;  // This is for the Task Class
using ABB.Robotics.Controllers.EventLogDomain;
using ABB.Robotics.Controllers.FileSystemDomain;

namespace Machina.Drivers.Communication
{
    /// <summary>
    /// This class acts as a bridge between Machina and the ABB controller, 
    /// using RobotStudio's SDK. 
    /// Ideally, this will be abstracted somewhere else in the future, so that
    /// Machina doesn't have this dependencies...
    /// </summary>
    class RobotStudioManager
    {
        private Driver _driver;  // the parent object

        // ABB stuff and flags
        private Controller controller;
        private ABB.Robotics.Controllers.RapidDomain.Task tRob1Task;
        private RobotWare robotWare;
        private RobotWareOptionCollection robotWareOptions;
        private int _deviceId; 
        private bool hasMultiTasking = false;
        private bool hasEGM = false;
        private bool isLogged = false;
        private bool isConnected = false;
        private bool isRunning = false;

        private const string REMOTE_BUFFER_DIR = "Machina";

        public RobotStudioManager(Driver parent)
        {
            _driver = parent;
        }


        /// <summary>
        /// Reverts the Comm object to a blank state before any connection attempt. 
        /// </summary>
        public void Disconnect()
        {
            StopProgramExecution(true);
            ReleaseIP();
            LogOff();
            ReleaseMainTask();
            ReleaseController();
            isConnected = false;
        }


        public bool Connect(int deviceId)
        {
            _deviceId = deviceId;

            // Connect to the ABB real/virtual controller
            if (!LoadController(deviceId))
            {
                Console.WriteLine("Could not connect to controller");
                Disconnect();
                return false;
            }

            // Load the controller's IP
            if (!LoadIP())
            {
                Console.WriteLine("Could not find the controller's IP");
                Disconnect();
                return false;
            }

            // Log on to the controller
            if (!LogOn())
            {
                Console.WriteLine("Could not log on to the controller");
                Disconnect();
                return false;
            }

            // Is controller in Automatic mode with motors on?
            if (!IsControllerInAutoMode())
            {
                Console.WriteLine("Please set up controller to AUTOMATIC MODE and try again.");
                Disconnect();
                return false;
            }

            if (!IsControllerMotorsOn())
            {
                Console.WriteLine("Please set up Motors On mode in controller");
                Disconnect();
                return false;
            }

            // Test if Rapid Mastership is available
            if (!TestMastershipRapid())
            {
                Console.WriteLine("Mastership not available");
                Disconnect();
                return false;
            }

            // Load main task from the controller
            if (!LoadMainTask())
            {
                Console.WriteLine("Could not load main task");
                Disconnect();
                return false;
            }

            if (!SetRunMode(CycleType.Once))
            {

            }

            // Subscribe to relevant events to keep track of robot execution
            if (!SubscribeToEvents())
            {
                Console.WriteLine("Could not subscribe to robot controller events");
                Disconnect();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Searches the network for a robot controller and establishes a connection with the specified one by position.
        /// Performs no LogOn actions or similar. 
        /// </summary>
        /// <returns></returns>
        private bool LoadController(int controllerID)
        {
            // Scan the network and hookup to the specified controller
            bool success = false;

            // This is specific to ABB, should become abstracted at some point...
            Console.WriteLine("Scanning the network for controllers...");
            NetworkScanner scanner = new NetworkScanner();
            ControllerInfo[] controllers = scanner.GetControllers();
            if (controllers.Length > 0)
            {
                int cId = controllerID > controllers.Length ? controllers.Length - 1 :
                    controllerID < 0 ? 0 : controllerID;
                controller = ControllerFactory.CreateFrom(controllers[cId]);
                if (controller != null)
                {
                    //isConnected = true;
                    Console.WriteLine($"Found controller {controller.SystemName} on {controller.Name}");
                    success = true;

                    this.robotWare = this.controller.RobotWare;
                    this.robotWareOptions = this.robotWare.Options;

                    this.hasMultiTasking = HasMultiTaskOption(this.robotWareOptions);
                    this.hasEGM = HasEGMOption(this.robotWareOptions);
                }
                else
                {
                    Console.WriteLine("Could not connect to controller...");
                }
            }
            else
            {
                Console.WriteLine("No controllers found on the network");
            }



            return success;
        }

        /// <summary>
        /// Disposes the controller object. This has to be done manually, since COM objects are not
        /// automatically garbage collected. 
        /// </summary>
        /// <returns></returns>
        private bool ReleaseController()
        {
            if (controller != null)
            {
                controller.Dispose();
                controller = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Load the controller's IP address into the object.
        /// </summary>
        /// <returns></returns>
        private bool LoadIP()
        {
            if (controller != null && controller.IPAddress != null)
            {
                this._driver.IP = controller.IPAddress.ToString();
                Console.WriteLine($"Loaded IP {this._driver.IP}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets IP address. 
        /// </summary>
        /// <returns></returns>
        private bool ReleaseIP()
        {
            this._driver.IP = "";
            return true;
        }


        /// <summary>
        /// Logs on to the controller with a default user.
        /// </summary>
        /// <returns></returns>
        private bool LogOn()
        {
            // Sanity
            if (isLogged) LogOff();

            if (controller != null)
            {
                try
                {
                    controller.Logon(UserInfo.DefaultUser);
                    Console.WriteLine("Logged on as DefaultUser");
                    isLogged = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not log on to the controller");
                    Console.WriteLine(ex);
                    isLogged = false;
                }
            }

            return isLogged;
        }

        /// <summary>
        /// Logs off from the controller.
        /// </summary>
        /// <returns></returns>
        private bool LogOff()
        {
            if (controller != null)
            {
                controller.Logoff();
            }
            isLogged = false;
            return true;
        }

        /// <summary>
        /// Returns true if controller is in automatic mode.
        /// </summary>
        /// <returns></returns>
        private bool IsControllerInAutoMode()
        {
            if (controller != null)
            {
                if (controller.OperatingMode == ControllerOperatingMode.Auto)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if controller has Motors On
        /// </summary>
        /// <returns></returns>
        private bool IsControllerMotorsOn()
        {
            if (controller != null)
            {
                if (controller.State == ControllerState.MotorsOn)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieves the main task from the ABB controller, typically 't_rob1'.
        /// </summary>
        /// <returns></returns>
        private bool LoadMainTask()
        {
            if (controller == null)
            {
                Console.WriteLine("Cannot retreive main task: no controller available");
                return false;
            }

            try
            {
                ABB.Robotics.Controllers.RapidDomain.Task[] tasks = controller.Rapid.GetTasks();
                if (tasks.Length > 0)
                {
                    tRob1Task = tasks[0];
                    Console.WriteLine("Retrieved task " + tRob1Task.Name);
                    return true;
                }
                else
                {
                    tRob1Task = null;
                    Console.WriteLine("Could not retrieve any task from the controller");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not retrieve main task from controller");
                Console.WriteLine(ex);
            }


            return false;
        }

        /// <summary>
        /// Disposes the task object. This has to be done manually, since COM objects are not
        /// automatically garbage collected. 
        /// </summary>
        /// <returns></returns>
        private bool ReleaseMainTask()
        {
            if (tRob1Task != null)
            {
                tRob1Task.Dispose();
                tRob1Task = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Pings the controller's Rapid resource with a bogus request to check if it is available for
        /// Mastership, or it is held by someone else.
        /// </summary>
        /// <returns></returns>
        private bool TestMastershipRapid()
        {
            if (controller != null)
            {
                try
                {
                    using (Mastership.Request(controller.Rapid))
                    {
                        // Gets the current execution cycle from the RAPID module and sets it back to the same value (just a stupid test)
                        ExecutionCycle mode = controller.Rapid.Cycle;
                        controller.Rapid.Cycle = mode;
                        Console.WriteLine("Mastership test OK");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Rapid Mastership not available");
                    Console.WriteLine(ex);
                }
            }
            else
            {
                Console.WriteLine("Cannot test Rapid Mastership, no controller available");
            }
            return false;
        }

        /// <summary>
        /// Subscribe to relevant events in the controller and assign them handlers.
        /// </summary>
        private bool SubscribeToEvents()
        {
            if (controller == null)
            {
                Console.WriteLine("Cannot subscribe to controller events: not connected to controller.");
            }
            else
            {
                // Suscribe to changes in the controller
                controller.OperatingModeChanged += OnOperatingModeChanged;
                controller.ConnectionChanged += OnConnectionChanged;
                //controller.MastershipChanged += OnMastershipChanged;
                controller.StateChanged += OnStateChanged;

                // Suscribe to Rapid program execution (Start, Stop...)
                controller.Rapid.ExecutionStatusChanged += OnRapidExecutionStatusChanged;

                // Suscribe to Mastership changes 
                controller.Rapid.MastershipChanged += OnRapidMastershipChanged;

                // Suscribe to Task Enabled changes
                controller.Rapid.TaskEnabledChanged += OnRapidTaskEnabledChanged;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Deletes all existing modules from main task in the controller. 
        /// </summary>
        /// <returns></returns>
        private int ClearAllModules()
        {
            if (controller == null)
            {
                Console.WriteLine("Cannot clear modules: not connected to controller");
                return -1;
            }

            if (tRob1Task == null)
            {
                Console.WriteLine("Cannot clear modules: main task not retrieved");
                return -1;
            }

            int count = -1;

            ABB.Robotics.Controllers.RapidDomain.Module[] modules = tRob1Task.GetModules();
            count = 0;

            try
            {
                using (Mastership.Request(controller.Rapid))
                {
                    foreach (ABB.Robotics.Controllers.RapidDomain.Module m in modules)
                    {
                        Console.WriteLine("Deleting module: {0}", m.Name);
                        m.Delete();
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CLEAR MODULES ERROR: {0}", ex);
            }

            Console.WriteLine("Cleared {0} modules from main task", count);
            return count;
        }


        /// <summary>
        /// Resets the program pointer in the controller to the main entry point. Needs to be called
        /// before starting execution of a program, otherwise the controller will throw an error. 
        /// </summary>
        internal bool ResetProgramPointer()
        {
            if (controller == null)
            {
                Console.WriteLine("Cannot reset pointer: not connected to controller");
                return false;
            }

            if (tRob1Task == null)
            {
                Console.WriteLine("Cannot reset pointer: mainTask not present");
                return false;
            }

            try
            {
                using (Mastership.Request(controller.Rapid))
                {
                    tRob1Task.ResetProgramPointer();
                    return true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot reset pointer...");
                Console.WriteLine(ex);
            }

            return false;
        }


        private bool HasMultiTaskOption(RobotWareOptionCollection options)
        {
            var available = false;
            foreach (RobotWareOption option in options)
            {
                if (option.Description.Contains("623-1"))
                {
                    available = true;
                    break;
                }
            }
            return available;
        }

        private bool HasEGMOption(RobotWareOptionCollection options)
        {
            var available = false;
            foreach (RobotWareOption option in options)
            {
                if (option.Description.Contains("689-1"))
                {
                    available = true;
                    break;
                }
            }
            return available;
        }


        /// <summary>
        /// Requests start executing the program in the main task. Remember to call ResetProgramPointer() before. 
        /// </summary>
        public bool StartProgramExecution()
        {
            if (!isConnected)
            {
                Console.WriteLine("Cannot start program: not connected to controller");
                return false;
            }

            if (isRunning)
            {
                Console.WriteLine("Program is already running...");
                return false;
            }

            if (!ResetProgramPointer())
            {
                Console.WriteLine("Cannot start program: cannot reset program pointer");
                return false;
            }

            try
            {
                using (Mastership.Request(controller.Rapid))
                {
                    bool isControllerRunning = controller.Rapid.ExecutionStatus == ExecutionStatus.Running;

                    if (isControllerRunning != isRunning)
                    {
                        throw new Exception("isRunning mismatch state");
                    }

                    StartResult res = controller.Rapid.Start(true);
                    if (res != StartResult.Ok)
                    {
                        Console.WriteLine($"Cannot start program: {res}");
                    }
                    else
                    {
                        isRunning = true;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("PROGRAM START ERROR: " + ex);
            }

            return false;
        }


        /// <summary>
        /// Requests stop executing the program in the main task.
        /// </summary>
        /// <param name="immediate">Stop right now or wait for current cycle to complete?</param>
        /// <returns></returns>
        public bool StopProgramExecution(bool immediate)
        {
            if (controller == null) return true;

            if (!isConnected)
            {
                Console.WriteLine("Cannot stop program: not connected to controller");
                return false;
            }

            if (!isRunning)
            {
                Console.WriteLine("Cannot stop program: execution is already stopped");
                return false;
            }

            try
            {
                using (Mastership.Request(controller.Rapid))
                {
                    controller.Rapid.Stop(immediate ? StopMode.Immediate : StopMode.Cycle);
                    isRunning = false;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not stop program...");
                Console.WriteLine(ex);
            }

            return false;
        }

        /// <summary>
        /// Sets the Rapid ExecutionCycle to Once, Forever or None.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool SetRunMode(CycleType mode)
        {
            if (!isConnected)
            {
                Console.WriteLine("Cannot set RunMode, not connected to any controller");
                return false;
            }

            try
            {
                using (Mastership.Request(controller.Rapid))
                {
                    controller.Rapid.Cycle = mode == CycleType.Once ? ExecutionCycle.Once : mode == CycleType.Loop ? ExecutionCycle.Forever : ExecutionCycle.None;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting RunMode in controller...");
                Console.WriteLine(ex);
            }

            return false;
        }

        /// <summary>
        /// Loads a module to the device from a text resource in the assembly, with a target name on the controller.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="targetName"></param>
        /// <returns></returns>
        public bool LoadModuleToController(string resourceName, string targetName)
        {
            if (!isConnected)
            {
                Console.WriteLine("Cannot load program, not connected to controller");
                return false;
            }

            string path = Path.Combine(Path.GetTempPath(), targetName);
            if (!Machina.IO.SaveTextResourceToFile(resourceName, path, Encoding.ASCII))
            {
                Console.WriteLine("Could not save module to temp file");
                return false;
            }
            else
            {
                Console.WriteLine("Saved module to " + path);
            }

            //if (!LoadFileToController(localBufferDirname, localBufferFilename, localBufferFileExtension))
            //{
            //    Console.WriteLine("Could not load module to controller");
            //    return false;
            //}

            if (!LoadFileToDevice(path))
            {
                Console.WriteLine("Could not load module to controller");
                return false;
            }

            return true;

        }

        /// <summary>
        /// Loads a module to the ABB controller given as a string list of Rapid code lines.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="programName"></param>
        /// <returns></returns>
        public bool LoadProgramToController(List<string> module, string programName)
        {
            if (!isConnected)
            {
                Console.WriteLine("Cannot load program, not connected to controller");
                return false;
            }

            string path = Path.Combine(Path.GetTempPath(), $"Machina_{programName}.mod");

            // Modules can only be uploaded to ABB controllers using a file
            if (!Machina.IO.SaveStringListToFile(module, path, Encoding.ASCII))  // 
            {
                Console.WriteLine("Could not save module to temp file");
                return false;
            }
            else
            {
                Console.WriteLine($"Saved {programName} to {path}");
            }

            if (!LoadFileToDevice(path))
            {
                Console.WriteLine("Could not load module to controller");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Loads a module into de controller from a local file. 
        /// @TODO: This is an expensive operation, should probably become threaded. 
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="wipeout"></param>
        /// <returns></returns>
        public bool LoadFileToDevice(string fullPath, bool wipeout = true)
        {

            string extension = Path.GetExtension(fullPath),     // ".mod"
                filename = Path.GetFileName(fullPath);          // "Machina_Server.mod"

            if (!isConnected)
            {
                Console.WriteLine("Could not load module '{0}', not connected to controller", fullPath);
                return false;
            }

            // check for correct ABB file extension
            if (!extension.ToLower().Equals(".mod"))
            {
                Console.WriteLine("Wrong file type, must use .mod files for ABB robots");
                return false;
            }

            // For the time being, we will always wipe out previous modules on load
            if (ClearAllModules() < 0)
            {
                Console.WriteLine("Error clearing modules");
                return false;
            }

            // Load the module
            bool success = false;
            try
            {
                using (Mastership.Request(controller.Rapid))
                {
                    // When connecting to a real controller, the reference filesystem 
                    // for Task.LoadModuleFromFile() becomes the controller's, so it is necessary
                    // to copy the file to the system first, and then load it. 

                    // Create the remoteBufferDirectory if applicable
                    FileSystem fs = controller.FileSystem;
                    string remotePath = fs.RemoteDirectory + "/" + REMOTE_BUFFER_DIR;
                    bool dirExists = fs.DirectoryExists(REMOTE_BUFFER_DIR);
                    if (!dirExists)
                    {
                        Console.WriteLine("Creating {0} on remote controller", remotePath);
                        fs.CreateDirectory(REMOTE_BUFFER_DIR);
                    }

                    //@TODO: Should implement some kind of file cleanup at somepoint...

                    // Copy the file to the remote controller
                    controller.FileSystem.PutFile(fullPath, $"{REMOTE_BUFFER_DIR}/{filename}", wipeout);
                    Console.WriteLine($"Copied {filename} to {REMOTE_BUFFER_DIR}");

                    // Loads a Rapid module to the task in the robot controller
                    success = tRob1Task.LoadModuleFromFile($"{remotePath}/{filename}", RapidLoadMode.Replace);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Could not load module");
                Console.WriteLine(ex);
            }

            // True if loading succeeds without any errors, otherwise false.  
            if (!success)
            {
                //// Gets the available categories of the EventLog. 
                //foreach (EventLogCategory category in controller.EventLog.GetCategories())
                //{
                //    if (category.Name == "Common")
                //    {
                //        if (category.Messages.Count > 0)
                //        {
                //            foreach (EventLogMessage message in category.Messages)
                //            {
                //                Console.WriteLine("Program [{1}:{2}({0})] {3} {4}",
                //                    message.Name, message.SequenceNumber,
                //                    message.Timestamp, message.Title, message.Body);
                //            }
                //        }
                //    }
                //}
            }
            else
            {
                Console.WriteLine("Sucessfully loaded {0}", fullPath);
            }

            return success;
        }

        
        /// <summary>
        /// Returns a Vector object representing the current robot's TCP position.
        /// </summary>
        /// <returns></returns>
        public Vector GetCurrentPosition()
        {
            if (!isConnected)
            {
                Console.WriteLine("Cannot GetCurrentPosition: not connected to controller");
                return null;
            }

            RobTarget rt = controller.MotionSystem.ActiveMechanicalUnit.GetPosition(ABB.Robotics.Controllers.MotionDomain.CoordinateSystemType.World);

            return new Vector(rt.Trans.X, rt.Trans.Y, rt.Trans.Z);
        }

        /// <summary>
        /// Returns a Rotation object representing the current robot's TCP orientation.
        /// </summary>
        /// <returns></returns>
        public Rotation GetCurrentOrientation()
        {
            if (!isConnected)
            {
                Console.WriteLine("Cannot GetCurrentRotation, not connected to controller");
                return null;
            }

            RobTarget rt = controller.MotionSystem.ActiveMechanicalUnit.GetPosition(ABB.Robotics.Controllers.MotionDomain.CoordinateSystemType.World);

            // ABB's convention is Q1..Q4 as W..Z
            return Rotation.FromQuaternion(rt.Rot.Q1, rt.Rot.Q2, rt.Rot.Q3, rt.Rot.Q4);
        }


        /// <summary>
        /// Returns a Joints object representing the rotations of the 6 axes of this robot.
        /// </summary>
        /// <returns></returns>
        public Joints GetCurrentJoints()
        {
            if (!isConnected)
            {
                Console.WriteLine("Cannot GetCurrentJoints, not connected to controller");
                return null;
            }

            try
            {
                JointTarget jt = controller.MotionSystem.ActiveMechanicalUnit.GetPosition();
                return new Joints(jt.RobAx.Rax_1, jt.RobAx.Rax_2, jt.RobAx.Rax_3, jt.RobAx.Rax_4, jt.RobAx.Rax_5, jt.RobAx.Rax_6);
            }
            catch (ABB.Robotics.Controllers.ServiceNotSupportedException e)
            {
                Console.WriteLine("CANNOT RETRIEVE JOINTS FROM CONTROLLER");
                Console.WriteLine(e);
                return null;
            }
        }

        /// <summary>
        /// Dumps a bunch of controller info to the console.
        /// </summary>
        public void DebugDump()
        {
            if (isConnected)
            {
                Console.WriteLine("");
                Console.WriteLine("DEBUG CONTROLLER DUMP:");
                Console.WriteLine("     AuthenticationSystem: " + controller.AuthenticationSystem.Name);
                Console.WriteLine("     BackupInProgress: " + controller.BackupInProgress);
                Console.WriteLine("     Configuration: " + controller.Configuration);
                Console.WriteLine("     Connected: " + controller.Connected);
                Console.WriteLine("     CurrentUser: " + controller.CurrentUser);
                Console.WriteLine("     DateTime: " + controller.DateTime);
                Console.WriteLine("     EventLog: " + controller.EventLog);
                Console.WriteLine("     FileSystem: " + controller.FileSystem);
                Console.WriteLine("     IOSystem: " + controller.IOSystem);
                Console.WriteLine("     IPAddress: " + controller.IPAddress);
                Console.WriteLine("     Ipc: " + controller.Ipc);
                Console.WriteLine("     IsMaster: " + controller.IsMaster);
                Console.WriteLine("     IsVirtual: " + controller.IsVirtual);
                Console.WriteLine("     MacAddress: " + controller.MacAddress);
                //Console.WriteLine("     MainComputerServiceInfo: ");
                //Console.WriteLine("         BoardType: " + controller.MainComputerServiceInfo.BoardType);
                //Console.WriteLine("         CpuInfo: " + controller.MainComputerServiceInfo.CpuInfo);
                //Console.WriteLine("         RamSize: " + controller.MainComputerServiceInfo.RamSize);
                //Console.WriteLine("         Temperature: " + controller.MainComputerServiceInfo.Temperature);
                Console.WriteLine("     MastershipPolicy: " + controller.MastershipPolicy);
                Console.WriteLine("     MotionSystem: " + controller.MotionSystem);
                Console.WriteLine("     Name: " + controller.Name);
                //Console.WriteLine("     NetworkSettings: " + controller.NetworkSettings);
                Console.WriteLine("     OperatingMode: " + controller.OperatingMode);
                Console.WriteLine("     Rapid: " + controller.Rapid);
                Console.WriteLine("     RobotWare: " + controller.RobotWare);
                Console.WriteLine("     RobotWareVersion: " + controller.RobotWareVersion);
                Console.WriteLine("     RunLevel: " + controller.RunLevel);
                Console.WriteLine("     State: " + controller.State);
                Console.WriteLine("     SystemId: " + controller.SystemId);
                Console.WriteLine("     SystemName: " + controller.SystemName);
                //Console.WriteLine("     TimeServer: " + controller.TimeServer);
                //Console.WriteLine("     TimeZone: " + controller.TimeZone);
                //Console.WriteLine("     UICulture: " + controller.UICulture);

                Console.WriteLine("");
                Console.WriteLine("DEBUG TASK DUMP:");
                Console.WriteLine("    Cycle: " + tRob1Task.Cycle);
                Console.WriteLine("    Enabled: " + tRob1Task.Enabled);
                Console.WriteLine("    ExecutionStatus: " + tRob1Task.ExecutionStatus);
                try
                {
                    Console.WriteLine("    ExecutionType: " + tRob1Task.ExecutionType);
                }
                catch (ABB.Robotics.Controllers.ServiceNotSupportedException e)
                {
                    Console.WriteLine("    ExecutionType: UNSUPPORTED BY CONTROLLER");
                    Console.WriteLine(e);
                }
                Console.WriteLine("    Motion: " + tRob1Task.Motion);
                Console.WriteLine("    MotionPointer: " + tRob1Task.MotionPointer.Module);
                Console.WriteLine("    Name: " + tRob1Task.Name);
                Console.WriteLine("    ProgramPointer: " + tRob1Task.ProgramPointer.Module);
                Console.WriteLine("    RemainingCycles: " + tRob1Task.RemainingCycles);
                Console.WriteLine("    TaskType: " + tRob1Task.TaskType);
                Console.WriteLine("    Type: " + tRob1Task.Type);
                Console.WriteLine("");

                Console.WriteLine("HAS MULTITASKING: " + this.hasMultiTasking);
                Console.WriteLine("HAS EGM: " + this.hasEGM);
                
            }
        }

        /// <summary>
        /// Loads the default StreamModule designed for streaming.
        /// </summary>
        internal bool LoadStreamingModule()
        {
            // TODO: depending on avilability of multitasking, upload different modules... 
            return LoadModuleToController("Machina.Resources.Modules.MachinaServerABB.txt", "Machina_Server.mod");
        }




        //███████╗██╗   ██╗███████╗███╗   ██╗████████╗    ██╗  ██╗ █████╗ ███╗   ██╗██████╗ ██╗     ██╗███╗   ██╗ ██████╗ 
        //██╔════╝██║   ██║██╔════╝████╗  ██║╚══██╔══╝    ██║  ██║██╔══██╗████╗  ██║██╔══██╗██║     ██║████╗  ██║██╔════╝ 
        //█████╗  ██║   ██║█████╗  ██╔██╗ ██║   ██║       ███████║███████║██╔██╗ ██║██║  ██║██║     ██║██╔██╗ ██║██║  ███╗
        //██╔══╝  ╚██╗ ██╔╝██╔══╝  ██║╚██╗██║   ██║       ██╔══██║██╔══██║██║╚██╗██║██║  ██║██║     ██║██║╚██╗██║██║   ██║
        //███████╗ ╚████╔╝ ███████╗██║ ╚████║   ██║       ██║  ██║██║  ██║██║ ╚████║██████╔╝███████╗██║██║ ╚████║╚██████╔╝
        //╚══════╝  ╚═══╝  ╚══════╝╚═╝  ╚═══╝   ╚═╝       ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝╚═════╝ ╚══════╝╚═╝╚═╝  ╚═══╝ ╚═════╝ 

        /// <summary>
        /// What to do when the robot starts running or stops.
        /// @TODO: add new behavior here when execution changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRapidExecutionStatusChanged(object sender, ExecutionStatusChangedEventArgs e)
        {
            Console.WriteLine("EXECUTION STATUS CHANGED: " + e.Status);

            if (e.Status == ExecutionStatus.Running)
            {
                isRunning = true;
            }
            else
            {
                isRunning = false;

                //// Only trigger Instruct queue
                //if (masterControl.GetControlMode() == ControlType.Execute)
                //{
                //    // Tick queue to move forward
                //    //masterControl.TriggerQueue();
                //    masterControl.TickWriteCursor();
                //}
            }
        }

        /// <summary>
        /// What to do when Mastership changes.
        /// @TODO: add behaviors...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRapidMastershipChanged(object sender, MastershipChangedEventArgs e)
        {
            Console.WriteLine("RAPID MASTERSHIP STATUS CHANGED: {0}", e.Status);

            // @TODO: what to do when mastership changes
        }

        /// <summary>
        /// What to do when the Task Enabled property changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRapidTaskEnabledChanged(object sender, TaskEnabledChangedEventArgs e)
        {
            Console.WriteLine("TASK ENABLED CHANGED: {0}", e.Enabled);

            // @TODO: add behaviors
        }

        /// <summary>
        /// What to do when the controller changes Operating Mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOperatingModeChanged(object sender, OperatingModeChangeEventArgs e)
        {
            Console.WriteLine("OPERATING MODE CHANGED: {0}", e.NewMode);
        }
        

        private void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            Console.WriteLine("CONTROLLER STATECHANGED: {0}", e.NewState);
        }

        private void OnMastershipChanged(object sender, MastershipChangedEventArgs e)
        {
            Console.WriteLine("CONTROLLER MASTERSHIP CHANGED: {0}", e.Status);
        }

        private void OnConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
            Console.WriteLine("CONTROLLER CONNECTION CHANGED: {0}", e.Connected);
        }



    }
}