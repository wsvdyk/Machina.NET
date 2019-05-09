﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Machina.Controllers;
using Machina.Drivers;

namespace Machina
{
    //   ██████╗ ██████╗ ███╗   ██╗████████╗██████╗  ██████╗ ██╗     
    //  ██╔════╝██╔═══██╗████╗  ██║╚══██╔══╝██╔══██╗██╔═══██╗██║     
    //  ██║     ██║   ██║██╔██╗ ██║   ██║   ██████╔╝██║   ██║██║     
    //  ██║     ██║   ██║██║╚██╗██║   ██║   ██╔══██╗██║   ██║██║     
    //  ╚██████╗╚██████╔╝██║ ╚████║   ██║   ██║  ██║╚██████╔╝███████╗
    //   ╚═════╝ ╚═════╝ ╚═╝  ╚═══╝   ╚═╝   ╚═╝  ╚═╝ ╚═════╝ ╚══════╝
    //                                                               

    /// <summary>
    /// The core class that centralizes all private control.
    /// </summary>
    class Control
    {
        // Some 'environment variables' to define check states and behavior
        public const bool SAFETY_STOP_IMMEDIATE_ON_DISCONNECT = true;         // when disconnecting from a controller, issue an immediate Stop request?

        // @TODO: move to cursors, make it device specific
        public const double DEFAULT_SPEED = 20;                                 // default speed for new actions in mm/s and deg/s
        public const double DEFAULT_ACCELERATION = 30;                          // default acc for new actions in mm/s^2 and deg/s^2; zero values let the controller figure out accelerations
        public const double DEFAULT_PRECISION = 5;                              // default precision for new actions

        public const MotionType DEFAULT_MOTION_TYPE = MotionType.Linear;        // default motion type for new actions
        public const ReferenceCS DEFAULT_REFCS = ReferenceCS.World;             // default reference coordinate system for relative transform actions
        public const ControlType DEFAULT_CONTROLMODE = ControlType.Offline;
        public const CycleType DEFAULT_RUNMODE = CycleType.Once;
        public const ConnectionType DEFAULT_CONNECTIONMODE = ConnectionType.User;



        /// <summary>
        /// Operation modes by default
        /// </summary>
        internal ControlType _controlMode;
        public ControlType ControlMode { get { return _controlMode; } internal set { _controlMode = value; } }
        internal ControlManager _controlManager;


        internal CycleType runMode = DEFAULT_RUNMODE;
        internal ConnectionType connectionMode;


        /// <summary>
        /// A reference to the Robot object this class is driving.
        /// </summary>
        internal Robot parentRobot;

        /// <summary>
        /// A reference to the parent Robot's Logger object.
        /// </summary>
        internal RobotLogger logger;

        /// <summary>
        /// Instances of the main robot Controller and Task
        /// </summary>
        private Driver _driver;
        internal Driver Driver { get { return _driver; } set { _driver = value; } }

        // Cursors
        private RobotCursor _issueCursor, _releaseCursor, _executionCursor, _motionCursor;

        /// <summary>
        /// A mutable alias for the cursor that will be used to return the most recent state for the robot,
        /// a.k.a. which cursor to use for sync GetJoints(), GetPose()-kind of functions...
        /// Mainly the issueCursor for Offline modes, executionCursor for Stream, etc.
        /// </summary>
        private RobotCursor _stateCursor;

        /// <summary>
        /// A virtual representation of the state of the device after application of issued actions.
        /// </summary>
        public RobotCursor IssueCursor => _issueCursor;

        /// <summary>
        /// A virtual representation of the state of the device after releasing pending actions to the controller.
        /// Keeps track of the state of an issue robot immediately following all the actions released from the 
        /// actionsbuffer to target device defined by controlMode, like an offline program, a full intruction execution 
        /// or a streamed target.
        /// </summary>
        public RobotCursor ReleaseCursor => _releaseCursor;

        /// <summary>
        /// A virtual representation of the state of the device after an action has been executed. 
        /// </summary>
        public RobotCursor ExecutionCursor => _executionCursor;

        /// <summary>
        /// A virtual representation of the state of the device tracked in pseudo real time. 
        /// Is independent from the other cursors, and gets updated (if available) at periodic intervals from the controller. 
        /// </summary>
        public RobotCursor MotionCursor => _motionCursor;

        /// <summary>
        /// Are cursors ready to start working?
        /// </summary>
        private bool _areCursorsInitialized = false;



        /// <summary>
        /// A shared instance of a Thread to manage sending and executing actions
        /// in the controller, which typically takes a lot of resources
        /// and halts program execution.
        /// </summary>
        private Thread actionsExecuter;


        //// @TODO: this will need to get reallocated when fixing stream mode...
        //public StreamQueue streamQueue;






        //██████╗ ██╗   ██╗██████╗ ██╗     ██╗ ██████╗
        //██╔══██╗██║   ██║██╔══██╗██║     ██║██╔════╝
        //██████╔╝██║   ██║██████╔╝██║     ██║██║     
        //██╔═══╝ ██║   ██║██╔══██╗██║     ██║██║     
        //██║     ╚██████╔╝██████╔╝███████╗██║╚██████╗
        //╚═╝      ╚═════╝ ╚═════╝ ╚══════╝╚═╝ ╚═════╝

        /// <summary>
        /// Main constructor.
        /// </summary>
        public Control(Robot parentBot)
        {
            parentRobot = parentBot;
            logger = parentRobot.logger;

            // Reset();

            _executionCursor = new RobotCursor(this, "ExecutionCursor", false, null);
            _releaseCursor = new RobotCursor(this, "ReleaseCursor", false, _executionCursor);
            _issueCursor = new RobotCursor(this, "IssueCursor", true, _releaseCursor);
            _issueCursor.LogRelativeActions = true;

            SetControlMode(DEFAULT_CONTROLMODE);
            SetConnectionMode(DEFAULT_CONNECTIONMODE);
        }

        ///// <summary>
        ///// Resets all internal state properties to default values. To be invoked upon
        ///// an internal robot reset.
        ///// @TODO rethink this
        ///// </summary>
        //public void Reset()
        //{
        //    virtualCursor = new RobotCursor(this, "virtualCursor", true);
        //    writeCursor = new RobotCursor(this, "writeCursor", false);
        //    virtualCursor.SetChild(writeCursor);
        //    motionCursor = new RobotCursor(this, "motionCursor", false);
        //    writeCursor.SetChild(motionCursor);
        //    areCursorsInitialized = false;

        //    SetControlMode(DEFAULT_CONTROLMODE);

        //    //currentSettings = new Settings(DEFAULT_SPEED, DEFAULT_ZONE, DEFAULT_MOTION_TYPE, DEFAULT_REFCS);
        //    //settingsBuffer = new SettingsBuffer();
        //}

        /// <summary>
        /// Sets current Control Mode and establishes communication if applicable.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool SetControlMode(ControlType mode)
        {
            //if (mode == ControlType.Execute)
            //{
            //    logger.Warning($"Execute mode temporarily deactivated. Try 'online' instead, it's cooler ;) ControlMode reverted to {_controlMode}");
            //    return false;
            //}

            _controlMode = mode;

            return ResetControl();
        }

        /// <summary>
        /// Resets control parameters using the appropriate ControlManager.
        /// </summary>
        /// <returns></returns>
        private bool ResetControl()
        {
            _controlManager = ControlFactory.GetControlManager(this);

            bool success = _controlManager.Initialize();

            if (ControlMode == ControlType.Offline)
            {
                InitializeRobotCursors(null, Rotation.FlippedAroundY, null);    // @TODO: this should depend on the Robot brand, model, cursor and so many other things... Added this quick fix to allow programs to start with MoveTo() instructions.
            }

            if (!success)
            {
                logger.Error("Couldn't SetControlMode()");
                throw new Exception("Couldn't SetControlMode()");
            }

            return success;
        }



        //private bool InitializeMode(ControlType mode)
        //{
        //    switch (mode) {
        //        case ControlType.Stream:
        //            return InitializeCommunication();
        //            break;

        //        case ControlType.Execute:
        //            // @TODO
        //            return false;
        //            break;

        //        // Offline
        //        default:
        //            if (comm != null) DropCommunication();
        //            // In offline modes, initialize the robot to a bogus standard transform
        //            return InitializeRobotCursors(new Vector(), Rotation.FlippedAroundY);  // @TODO: defaults should depend on robot make/model
        //            break;
        //    }
        //}


        ///// <summary>
        ///// Returns current Control Mode.
        ///// </summary>
        ///// <returns></returns>
        //public ControlType GetControlMode()
        //{
        //    return _controlMode;
        //}

        ///// <summary>
        ///// Sets current RunMode. 
        ///// </summary>
        ///// <param name="mode"></param>
        ///// <returns></returns>
        //public bool SetRunMode(CycleType mode)
        //{
        //    runMode = mode;

        //    if (controlMode == ControlType.Offline)
        //    {
        //        Console.WriteLine($"Remember RunMode.{mode} will have no effect in Offline mode");
        //    }
        //    else
        //    {
        //        return comm.SetRunMode(mode);
        //    }

        //    return false;
        //}

        ///// <summary>
        ///// Returns current RunMode.
        ///// </summary>
        ///// <param name="mode"></param>
        ///// <returns></returns>
        //public CycleType GetRunMode(CycleType mode)
        //{
        //    return runMode;
        //}


        internal bool ConfigureBuffer(int minActions, int maxActions)
        {
            return this._driver.ConfigureBuffer(minActions, maxActions);
        }


        /// <summary>
        /// Sets the current ConnectionManagerType.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool SetConnectionMode(ConnectionType mode)
        {
            if (_driver == null)
            {
                throw new Exception("Missing Driver object");
            }

            if (!_driver.AvailableConnectionTypes[mode])
            {
                logger.Warning($"This device's driver does not accept ConnectionType {mode}, ConnectionMode remains {this.connectionMode}");
                return false;
            }

            this.connectionMode = mode;

            return ResetControl();
        }



        /// <summary>
        /// Searches the network for a robot controller and establishes a connection with the specified one by position. 
        /// Necessary for "online" modes.
        /// </summary>
        /// <returns></returns>
        public bool ConnectToDevice(int robotId)
        {
            if (connectionMode == ConnectionType.User)
            {
                logger.Error("Cannot search for robots automatically, please use ConnectToDevice(ip, port) instead");
                return false;
            }

            // Sanity
            if (!_driver.ConnectToDevice(robotId))
            {
                logger.Error("Cannot connect to device");
                return false;
            }
            else
            {
                //SetRunMode(runMode);

                //// If successful, initialize robot cursors to mirror the state of the device
                //Vector currPos = _comm.GetCurrentPosition();
                //Rotation currRot = _comm.GetCurrentOrientation();
                //Joints currJnts = _comm.GetCurrentJoints();
                //InitializeRobotCursors(currPos, currRot, currJnts);

                // If successful, initialize robot cursors to mirror the state of the device.
                // The function will initialize them based on the _comm object.
                InitializeRobotCursors();
            }

            logger.Info("Connected to " + parentRobot.Brand + " robot \"" + parentRobot.Name + "\" on " + _driver.IP + ":" + _driver.Port);

            return true;
        }

        public bool ConnectToDevice(string ip, int port)
        {
            if (connectionMode == ConnectionType.Machina)
            {
                logger.Error("Try ConnectToDevice() instead");
                return false;
            }

            // Sanity
            if (!_driver.ConnectToDevice(ip, port))
            {
                logger.Error("Cannot connect to device");
                return false;
            }
            else
            {
                InitializeRobotCursors();
            }

            logger.Info("Connected to " + parentRobot.Brand + " robot \"" + parentRobot.Name + "\" on " + _driver.IP + ":" + _driver.Port);
            logger.Verbose("TCP:");
            logger.Verbose("  " + this.IssueCursor.position.ToString(true));
            logger.Verbose("  " + new Orientation(this.IssueCursor.rotation).ToString(true));
            logger.Verbose("  " + this.IssueCursor.axes.ToString(true));
            if (this.IssueCursor.externalAxesCartesian != null)
            {
                logger.Verbose("External Axes (TCP):");
                logger.Verbose("  " + this.IssueCursor.externalAxesCartesian.ToString(true));
            }
            if (this.IssueCursor.externalAxesJoints != null)
            {
                logger.Verbose("External Axes (J): ");
                logger.Verbose("  " + this.IssueCursor.externalAxesJoints.ToString(true));
            }
            return true;
        }



        /// <summary>
        /// Requests the Communication object to disconnect from controller and reset.
        /// </summary>
        /// <returns></returns>
        public bool DisconnectFromDevice()
        {
            bool result = _driver.DisconnectFromDevice();
            if (result)
            {
                logger.Info("Disconnected from " + parentRobot.Brand + " robot \"" + parentRobot.Name + "\"");
            }
            else
            {
                logger.Warning("Could not disconnect from " + parentRobot.Brand + " robot \"" + parentRobot.Name + "\"");
            }

            return result;
        }

        /// <summary>
        /// Is this robot connected to a real/virtual device?
        /// </summary>
        /// <returns></returns>
        public bool IsConnectedToDevice()
        {
            return _driver.Connected;
        }

        /// <summary>
        /// Sets the creddentials for logging into the controller.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool SetUserCredentials(string name, string password) =>
            _driver == null ? false : _driver.SetUser(name, password);

        /// <summary>
        /// If connected to a device, return the IP address
        /// </summary>
        /// <returns></returns>
        public string GetControllerIP() => _driver.IP;

        ///// <summary>
        ///// Loads a programm to the connected device and executes it. 
        ///// </summary>
        ///// <param name="programLines">A string list representation of the program's code.</param>
        ///// <param name="programName">Program name</param>
        ///// <returns></returns>
        //public bool LoadProgramToDevice(List<string> programLines, string programName = "Program")
        //{
        //    return comm.LoadProgramToController(programLines, programName);
        //}

        ///// <summary>
        ///// Loads a programm to the connected device and executes it. 
        ///// </summary>
        ///// <param name="filepath">Full filepath including root, directory structure, filename and extension.</param>
        ///// <param name="wipeout">Delete all previous modules in the device?</param>
        ///// <returns></returns>
        //public bool LoadProgramToDevice(string filepath, bool wipeout)
        //{
        //    if (controlMode == ControlType.Offline)
        //    {
        //        Console.WriteLine("Cannot load modules in Offline mode");
        //        return false;
        //    }

        //    // Sanity
        //    string fullPath = "";

        //    // Is the filepath a valid Windows path?
        //    try
        //    {
        //        fullPath = System.IO.Path.GetFullPath(filepath);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("'{0}' is not a valid filepath", filepath);
        //        Console.WriteLine(e);
        //        return false;
        //    }

        //    // Is it an absolute path?
        //    try
        //    {
        //        bool absolute = System.IO.Path.IsPathRooted(fullPath);
        //        if (!absolute)
        //        {
        //            Console.WriteLine("Relative paths are currently not supported");
        //            return false;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("'{0}' is not a valid absolute filepath", filepath);
        //        Console.WriteLine(e);
        //        return false;
        //    }

        //    //// Split the full path into directory, file and extension names
        //    //string dirname;     // full directory path
        //    //string filename;    // filename without extension
        //    //string extension;   // file extension

        //    //string[] parts = fullPath.Split('\\');
        //    //int len = parts.Length;
        //    //if (len < 2)
        //    //{
        //    //    Console.WriteLine("Weird filepath");
        //    //    return false;
        //    //}
        //    //dirname = string.Join("\\", parts, 0, len - 1);
        //    //string[] fileparts = parts[len - 1].Split('.');
        //    //filename = fileparts.Length > 2 ? string.Join(".", fileparts, 0, fileparts.Length - 1) : fileparts[0];  // account for filenames with multiple dots
        //    //extension = fileparts[fileparts.Length - 1];

        //    //Console.WriteLine("  filename: " + filename);
        //    //Console.WriteLine("  dirname: " + dirname);
        //    //Console.WriteLine("  extension: " + extension);

        //    //return comm.LoadFileToController(dirname, filename, extension, true);
        //    return comm.LoadFileToDevice(fullPath, wipeout);
        //}

        ///// <summary>
        ///// Triggers program start on device.
        ///// </summary>
        ///// <returns></returns>
        //public bool StartProgramOnDevice()
        //{
        //    if (controlMode == ControlType.Offline)
        //    {
        //        Console.WriteLine("No program to start in Offline mode");
        //        return false;
        //    }

        //    return comm.StartProgramExecution();
        //}

        ///// <summary>
        ///// Stops execution of running program on device.
        ///// </summary>
        ///// <param name="immediate"></param>
        ///// <returns></returns>
        //public bool StopProgramOnDevice(bool immediate)
        //{
        //    if (controlMode == ControlType.Offline)
        //    {
        //        Console.WriteLine("No program to stop in Offline mode");
        //        return false;
        //    }

        //    return comm.StopProgramExecution(immediate);
        //}


        //public Vector GetVirtualPosition() => IssueCursor.position;
        //public Rotation GetVirtualRotation() => IssueCursor.rotation;
        //public Joints GetVirtualAxes() => IssueCursor.joints;
        //public Tool GetVirtualTool() => IssueCursor.tool;


        
        internal Dictionary<string, string> GetDeviceDriverModules(Dictionary<string, string> parameters)
        {
            if (_controlMode == ControlType.Offline)
            {
                logger.Warning("Could not retrieve driver modules in Offline mode, must define a ConnectionMode first.");
                return null;
            }

            return _driver.GetDeviceDriverModules(parameters);
        }





        /// <summary>
        /// Returns a Vector object representing the current robot's TCP position.
        /// </summary>
        /// <returns></returns>
        public Vector GetCurrentPosition() => _stateCursor.position;

        /// <summary>
        /// Returns a Rotation object representing the current robot's TCP orientation.
        /// </summary>
        /// <returns></returns>
        public Rotation GetCurrentRotation() => _stateCursor.rotation;

        /// <summary>
        /// Returns a Joints object representing the rotations of the 6 axes of this robot.
        /// </summary>
        /// <returns></returns>
        public Joints GetCurrentAxes() => _stateCursor.axes;

        /// <summary>
        /// Returns a double?[] array representing the values for the external axes.
        /// </summary>
        /// <returns></returns>
        public ExternalAxes GetCurrentExternalAxes() => _stateCursor.externalAxesCartesian;



        /// <summary>
        /// Gets current speed setting.
        /// </summary>
        /// <returns></returns>
        public double GetCurrentSpeedSetting() => _stateCursor.speed;

        /// <summary>
        /// Gets current scceleration setting.
        /// </summary>
        /// <returns></returns>
        public double GetCurrentAccelerationSetting() => _stateCursor.acceleration;

        /// <summary>
        /// Gets current precision setting.
        /// </summary>
        /// <returns></returns>
        public double GetCurrentPrecisionSetting() => _stateCursor.precision;

        /// <summary>
        /// Gets current Motion setting.
        /// </summary>
        /// <returns></returns>
        public MotionType GetCurrentMotionTypeSetting() => _stateCursor.motionType;

        /// <summary>
        /// Gets the reference coordinate system used for relative transform actions.
        /// </summary>
        /// <returns></returns>
        public ReferenceCS GetCurrentReferenceCS()
        {
            return IssueCursor.referenceCS;
        }

        /// <summary>
        /// Returns a Tool object representing the currently attached tool, null if none.
        /// </summary>
        /// <returns></returns>
        public Tool GetCurrentTool() => _stateCursor.tool;






        /// <summary>
        /// For Offline modes, it flushes all pending actions and returns a devide-specific program 
        /// as a stringList representation.
        /// </summary>
        /// <param name="inlineTargets">Write inline targets on action statements, or declare them as independent variables?</param>
        /// <param name="humanComments">If true, a human-readable description will be added to each line of code</param>
        /// <returns></returns>
        public List<string> Export(bool inlineTargets, bool humanComments)
        {
            if (_controlMode != ControlType.Offline)
            {
                logger.Warning("Export() only works in Offline mode");
                return null;
            }

            //List<Action> actions = actionBuffer.GetAllPending();
            //return programGenerator.UNSAFEProgramFromActions("BRobotProgram", writeCursor, actions);

            return ReleaseCursor.ProgramFromBuffer(inlineTargets, humanComments);
        }

        /// <summary>
        /// For Offline modes, it flushes all pending actions and exports them to a robot-specific program as a text file.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="inlineTargets">Write inline targets on action statements, or declare them as independent variables?</param>
        /// <param name="humanComments">If true, a human-readable description will be added to each line of code</param>
        /// <returns></returns>
        public bool Export(string filepath, bool inlineTargets, bool humanComments)
        {
            // @TODO: add some filepath sanity here

            List<string> programCode = Export(inlineTargets, humanComments);
            if (programCode == null) return false;
            return SaveStringListToFile(programCode, filepath);
        }

        /// <summary>
        /// In 'execute' mode, flushes all pending actions, creates a program, 
        /// uploads it to the controller and runs it.
        /// </summary>
        /// <returns></returns>
        public void Execute()
        {
            //if (_controlMode != ControlType.Execute)
            //{
            //    Console.WriteLine("Execute() only works in Execute mode");
            //    return;
            //}

            //writeCursor.QueueActions();
            //TickWriteCursor();

            throw new NotImplementedException();
        }

        //   █████╗  ██████╗████████╗██╗ ██████╗ ███╗   ██╗███████╗
        //  ██╔══██╗██╔════╝╚══██╔══╝██║██╔═══██╗████╗  ██║██╔════╝
        //  ███████║██║        ██║   ██║██║   ██║██╔██╗ ██║███████╗
        //  ██╔══██║██║        ██║   ██║██║   ██║██║╚██╗██║╚════██║
        //  ██║  ██║╚██████╗   ██║   ██║╚██████╔╝██║ ╚████║███████║
        //  ╚═╝  ╚═╝ ╚═════╝   ╚═╝   ╚═╝ ╚═════╝ ╚═╝  ╚═══╝╚══════╝
        //                                                         

        /// <summary>
        /// Issue an Action of whatever kind...
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public bool IssueApplyActionRequest(Action action)
        {
            if (!_areCursorsInitialized)
            {
                logger.Error("Cursors not initialized. Did you .Connect()?");
                return false;
            }

            bool success = IssueCursor.Issue(action);

            if (success) RaiseActionIssuedEvent();

            return success;
        }

        //  ███╗   ███╗██╗   ██╗     ██████╗ ██╗    ██╗███╗   ██╗    
        //  ████╗ ████║╚██╗ ██╔╝    ██╔═══██╗██║    ██║████╗  ██║    
        //  ██╔████╔██║ ╚████╔╝     ██║   ██║██║ █╗ ██║██╔██╗ ██║    
        //  ██║╚██╔╝██║  ╚██╔╝      ██║   ██║██║███╗██║██║╚██╗██║    
        //  ██║ ╚═╝ ██║   ██║       ╚██████╔╝╚███╔███╔╝██║ ╚████║    
        //  ╚═╝     ╚═╝   ╚═╝        ╚═════╝  ╚══╝╚══╝ ╚═╝  ╚═══╝   

        /// <summary>
        /// Issue a test1 action request with robtargets in RAPID code
        /// </summary>
        /// <param name="relative"></param>
        /// <returns></returns>
        public bool IssueTest1Request(bool relative) =>
            IssueApplyActionRequest(new ActionTest1(relative));

        /// <summary>
        /// Issue a test2 action request with robtargets in RAPID code
        /// </summary>
        /// <param name="relative"></param>
        /// <returns></returns>
        public bool IssueTest2Request(bool relative) =>
            IssueApplyActionRequest(new ActionTest2(relative));

        /// <summary>
        /// Issue a complete move action request that uses new settings.
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        public bool IssueMoveToRobTargetRequest(Vector trans, double q1, double q2, double q3, double q4, double cf1, double cf4, double cf6, double cfX, bool relative) =>
            IssueApplyActionRequest(new ActionMoveToRobTarget(trans, q1, q2, q3, q4, cf1, cf4, cf6, cfX, relative));

        public bool IssueMovecToRobTargetRequest(Vector trans1, double[,] data1, Vector trans2, double[,] data2, bool relative) =>
            IssueApplyActionRequest(new ActionMovecToRobTarget(trans1, data1, trans2, data2, relative));

        public bool IssueAbbDefineToolRequest(string name, Vector positie, double[] orient, double weight, double cogX, double cogY, double cogZ) =>
            IssueApplyActionRequest(new ActionAbbDefineTool(name, positie, orient, weight, cogX, cogY, cogZ));

        public bool IssueAbbAttachToolRequest(string name) =>
            IssueApplyActionRequest(new ActionAbbAttachTool(name));



        //  ███████╗██╗  ██╗██╗███████╗████████╗██╗███╗   ██╗ ██████╗ 
        //  ██╔════╝╚██╗██╔╝██║██╔════╝╚══██╔══╝██║████╗  ██║██╔════╝ 
        //  █████╗   ╚███╔╝ ██║███████╗   ██║   ██║██╔██╗ ██║██║  ███╗
        //  ██╔══╝   ██╔██╗ ██║╚════██║   ██║   ██║██║╚██╗██║██║   ██║
        //  ███████╗██╔╝ ██╗██║███████║   ██║   ██║██║ ╚████║╚██████╔╝
        //  ╚══════╝╚═╝  ╚═╝╚═╝╚══════╝   ╚═╝   ╚═╝╚═╝  ╚═══╝ ╚═════╝   
        public bool IssueSpeedRequest(double speed, bool relative) => 
                IssueApplyActionRequest(new ActionSpeed(speed, relative));

        public bool IssueAccelerationRequest(double acc, bool relative) => 
                IssueApplyActionRequest(new ActionAcceleration(acc, relative));
        

        public bool IssuePrecisionRequest(double precision, bool relative) =>
                IssueApplyActionRequest(new ActionPrecision(precision, relative));


        public bool IssueMotionRequest(MotionType motionType) =>
                IssueApplyActionRequest(new ActionMotionMode(motionType));


        public bool IssueCoordinatesRequest(ReferenceCS referenceCS) =>
                IssueApplyActionRequest(new ActionCoordinates(referenceCS));


        public bool IssuePushPopRequest(bool push) =>
                IssueApplyActionRequest(new ActionPushPop(push));


        public bool IssueTemperatureRequest(double temp, RobotPartType robotPart, bool waitToReachTemp, bool relative) =>
                IssueApplyActionRequest(new ActionTemperature(temp, robotPart, waitToReachTemp, relative));


        public bool IssueExtrudeRequest(bool extrude) =>
                IssueApplyActionRequest(new ActionExtrusion(extrude));


        public bool IssueExtrusionRateRequest(double rate, bool relative) =>
                IssueApplyActionRequest(new ActionExtrusionRate(rate, relative));

        /// <summary>
        /// Issue a Translation action request that falls back on the state of current settings.
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        public bool IssueTranslationRequest(Vector trans, bool relative) =>
                IssueApplyActionRequest(new ActionTranslation(trans, relative));


        /// <summary>
        /// Issue a Rotation action request with fully customized parameters.
        /// </summary>
        /// <param name="rot"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        public bool IssueRotationRequest(Rotation rot, bool relative) =>
                IssueApplyActionRequest(new ActionRotation(rot, relative));


        /// <summary>
        /// Issue a Translation + Rotation action request with fully customized parameters.
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="rot"></param>
        /// <param name="rel"></param>
        /// <param name="translationFirst"></param>
        /// <returns></returns>
        public bool IssueTransformationRequest(Vector trans, Rotation rot, bool rel, bool translationFirst) =>
                IssueApplyActionRequest(new ActionTransformation(trans, rot, rel, translationFirst));


        /// <summary>
        /// Issue a request to set the values of joint angles in configuration space. 
        /// </summary>
        /// <param name="joints"></param>
        /// <param name="relJnts"></param>
        /// <param name="speed"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        public bool IssueJointsRequest(Joints joints, bool relJnts) =>
                IssueApplyActionRequest(new ActionAxes(joints, relJnts));


        /// <summary>
        /// Issue a request to display a string message on the device.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool IssueMessageRequest(string message) =>
                IssueApplyActionRequest(new ActionMessage(message));


        /// <summary>
        /// Issue a request for the device to stay idle for a certain amount of time.
        /// </summary>
        /// <param name="millis"></param>
        /// <returns></returns>
        public bool IssueWaitRequest(long millis) =>
                IssueApplyActionRequest(new ActionWait(millis));


        /// <summary>
        /// Issue a request to add an internal comment in the compiled code. 
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public bool IssueCommentRequest(string comment) =>
                IssueApplyActionRequest(new ActionComment(comment));


        /// <summary>
        /// Issue a reques to defin a Tool in the robot's internal library, avaliable for Attach/Detach requests.
        /// </summary>
        /// <param name="tool"></param>
        /// <returns></returns>
        public bool IssueDefineToolRequest(Tool tool) =>
                IssueApplyActionRequest(new ActionDefineTool(tool));


        /// <summary>
        /// Issue a request to attach a Tool to the flange of the robot
        /// </summary>
        /// <param name="tool"></param>
        /// <returns></returns>
        public bool IssueAttachRequest(string toolName) =>
                IssueApplyActionRequest(new ActionAttachTool(toolName));


        /// <summary>
        /// Issue a request to return the robot to no tools attached. 
        /// </summary>
        /// <returns></returns>
        public bool IssueDetachRequest() =>
                IssueApplyActionRequest(new ActionDetachTool());


        /// <summary>
        /// Issue a request to turn digital IO on/off.
        /// </summary>
        /// <param name="pinId"></param>
        /// <param name="isOn"></param>
        /// <returns></returns>
        public bool IssueWriteToDigitalIORequest(string pinId, bool isOn, bool toolPin) =>
                IssueApplyActionRequest(new ActionIODigital(pinId, isOn, toolPin));


        /// <summary>
        /// Issue a request to write to analog pin.
        /// </summary>
        /// <param name="pinId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IssueWriteToAnalogIORequest(string pinId, double value, bool toolPin) =>
                IssueApplyActionRequest(new ActionIOAnalog(pinId, value, toolPin));


        /// <summary>
        /// Issue a request to add common initialization/termination procedures on the device, 
        /// like homing, calibration, fans, etc.
        /// </summary>
        /// <param name="initiate"></param>
        /// <returns></returns>
        public bool IssueInitializationRequest(bool initiate) =>
                IssueApplyActionRequest(new ActionInitialization(initiate));


        /// <summary>
        /// Issue a request to modify a external axis in this robot.
        /// Note axisNumber is one-based, i.e. axisNumber 1 is _externalAxes[0]
        /// </summary>
        /// <param name="axisNumber"></param>
        /// <param name="value"></param>
        /// <param name="target"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        public bool IssueExternalAxisRequest(int axisNumber, double value, ExternalAxesTarget target, bool relative) =>
                IssueApplyActionRequest(new ActionExternalAxis(axisNumber, value, target, relative));

        /// <summary>
        /// Issue a request to modify the arm-angle value for 7-dof robotic arms. 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        public bool IssueArmAngleRequest(double value, bool relative) =>
            IssueApplyActionRequest(new ActionArmAngle(value, relative));


        /// <summary>
        /// Issue a request to add custom code to a compiled program.
        /// </summary>
        /// <param name="statement"></param>
        /// <param name="isDeclaration"></param>
        /// <returns></returns>
        public bool IssueCustomCodeRequest(string statement, bool isDeclaration) =>
                IssueApplyActionRequest(new ActionCustomCode(statement, isDeclaration));







        //██████╗ ██████╗ ██╗██╗   ██╗ █████╗ ████████╗███████╗
        //██╔══██╗██╔══██╗██║██║   ██║██╔══██╗╚══██╔══╝██╔════╝
        //██████╔╝██████╔╝██║██║   ██║███████║   ██║   █████╗  
        //██╔═══╝ ██╔══██╗██║╚██╗ ██╔╝██╔══██║   ██║   ██╔══╝  
        //██║     ██║  ██║██║ ╚████╔╝ ██║  ██║   ██║   ███████╗
        //╚═╝     ╚═╝  ╚═╝╚═╝  ╚═══╝  ╚═╝  ╚═╝   ╚═╝   ╚══════╝

        // THIS IS NOW A TASK FOR THE ControlManager.SetCommunicationObject()
        /// <summary>
        /// Initializes the Communication object.
        /// </summary>
        /// <returns></returns>
        //private bool InitializeCommunication()
        //{
        //    Console.WriteLine("InitializeCommunication");

        //    // If there is already some communication going on
        //    if (_driver != null)
        //    {
        //        Console.WriteLine("Communication protocol might be active. Please CloseControllerCommunication() first.");
        //        return false;
        //    }

        //    // @TODO: shim assignment of correct robot model/brand
        //    //_driver = new DriverABB(this);
        //    if (this.parentRobot.Brand == RobotType.ABB)
        //    {
        //        _driver = new DriverABB(this);
        //    }
        //    else if (this.parentRobot.Brand == RobotType.UR)
        //    {
        //        _driver = new DriverUR(this);
        //    }
        //    else
        //    {
        //        throw new NotImplementedException();
        //    }

        //    // Pass the streamQueue object as a shared reference
        //    //comm.LinkStreamQueue(streamQueue);
        //    if (_controlMode == ControlType.Stream)
        //    {
        //        _driver.LinkWriteCursor(ref writeCursor);
        //    }

        //    return true;
        //}

        /// <summary>
        /// Disconnects and resets the Communication object.
        /// </summary>
        /// <returns></returns>
        private bool DropCommunication()
        {
            if (_driver == null)
            {
                logger.Debug("Communication protocol not established, no DropCommunication() performed.");
                return false;
            }
            bool success = _driver.DisconnectFromDevice();
            _driver = null;
            return success;
        }

        ///// <summary>
        ///// If there was a running Communication protocol, drop it and restart it again.
        ///// </summary>
        ///// <returns></returns>
        //private bool ResetCommunication()
        //{
        //    if (_driver == null)
        //    {
        //        Console.WriteLine("Communication protocol not established, please initialize first.");
        //    }
        //    DropCommunication();
        //    return InitializeCommunication();
        //}

        /// <summary>
        /// Initializes all instances of robotCursors with base information
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="joints"></param>
        /// <returns></returns>
        internal bool InitializeRobotCursors(Point position = null, Rotation rotation = null, Joints joints = null, ExternalAxes extAx = null,
            double speed = Control.DEFAULT_SPEED, double acc = Control.DEFAULT_ACCELERATION, double precision = Control.DEFAULT_PRECISION,
            MotionType mType = Control.DEFAULT_MOTION_TYPE, ReferenceCS refCS = Control.DEFAULT_REFCS)

        {
            bool success = true;
            success &= IssueCursor.Initialize(position, rotation, joints, extAx, speed, acc, precision, mType, refCS);
            success &= ReleaseCursor.Initialize(position, rotation, joints, extAx, speed, acc, precision, mType, refCS);
            success &= ExecutionCursor.Initialize(position, rotation, joints, extAx, speed, acc, precision, mType, refCS);

            _areCursorsInitialized = success;

            return success;
        }


        internal bool InitializeRobotCursors()
        {
            if (_driver == null)
            {
                throw new Exception("Cannot initialize Robotcursors without a _comm object");
            }

            // If successful, initialize robot cursors to mirror the state of the device
            Vector currPos = _driver.GetCurrentPosition();
            Rotation currRot = _driver.GetCurrentOrientation();
            Joints currJnts = _driver.GetCurrentJoints();
            ExternalAxes currExtAx = _driver.GetCurrentExternalAxes();

            return InitializeRobotCursors(currPos, currRot, currJnts, currExtAx);
        }



        internal bool InitializeMotionCursor()
        {
            _motionCursor = new RobotCursor(this, "MotionCursor", false, null);
            //_motionCursor.Initialize();  // No need for this, since this is just a "zombie" cursor, a holder of static properties updated in real-time with no actions applied to it. Any init info is negligible.
            return true;
        }


        /// <summary>
        /// Saves a string List to a file.
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        internal bool SaveStringListToFile(List<string> lines, string filepath)
        {
            try
            {
                System.IO.File.WriteAllLines(filepath, lines, this.parentRobot.Brand == RobotType.HUMAN ? Encoding.UTF8 : Encoding.ASCII);  // human compiler works better at UTF8, but this was ASCII for ABB controllers, right??
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Could not save program to file...");
                logger.Error(ex);
            }
            return false;
        }

        /// <summary>
        /// Sets which cursor to use as most up-to-date tracker.
        /// </summary>
        /// <param name="cursor"></param>
        internal void SetStateCursor(RobotCursor cursor)
        {
            this._stateCursor = cursor;
        }





        ///// <summary>
        ///// Triggers a thread to send instructions to the connected device if applicable. 
        ///// </summary>
        //public void TickWriteCursor()
        //{
        //    if (_controlMode == ControlType.Execute)
        //    {
        //        if (!_comm.IsRunning() && _areCursorsInitialized && writeCursor.AreActionsPending() && (actionsExecuter == null || !actionsExecuter.IsAlive))
        //        {
        //            actionsExecuter = new Thread(() => RunActionsBlockInController(true, false));  // http://stackoverflow.com/a/3360582
        //            actionsExecuter.Start();
        //        }
        //    }
        //    //else if (controlMode == ControlMode.Stream)
        //    //{
        //    //    comm.TickStreamQueue(true);
        //    //}
        //    else
        //    {
        //        Console.WriteLine("Nothing to tick here");
        //    }
        //}

        ///// <summary>
        ///// Creates a program with the first block of Actions in the cursor, uploads it to the controller
        ///// and runs it. 
        ///// </summary>
        //private void RunActionsBlockInController(bool inlineTargets, bool humanComments)
        //{
        //    List<string> program = writeCursor.ProgramFromBlock(inlineTargets, humanComments);
        //    _comm.LoadProgramToController(program, "Buffer");
        //    _comm.StartProgramExecution();
        //}








        ////██╗    ██╗██╗██████╗ 
        ////██║    ██║██║██╔══██╗
        ////██║ █╗ ██║██║██████╔╝
        ////██║███╗██║██║██╔═══╝ 
        ////╚███╔███╔╝██║██║     
        //// ╚══╝╚══╝ ╚═╝╚═╝     

        ///// <summary>
        ///// Adds a path to the queue manager and tick it for execution.
        ///// </summary>
        ///// <param name="path"></param>
        //public void AddPathToQueue(Path path)
        //{
        //    queue.Add(path);
        //    TriggerQueue();
        //}

        ///// <summary>
        ///// Checks the state of the execution of the robot, and if stopped and if elements 
        ///// remaining on the queue, starts executing them.
        ///// </summary>
        //public void TriggerQueue()
        //{
        //    if (!comm.IsRunning() && queue.ArePathsPending() && (pathExecuter == null || !pathExecuter.IsAlive))
        //    {
        //        Path path = queue.GetNext();
        //        // RunPath(path);

        //        // https://msdn.microsoft.com/en-us/library/aa645740(v=vs.71).aspx
        //        // Thread oThread = new Thread(new ThreadStart(oAlpha.Beta));
        //        // http://stackoverflow.com/a/3360582
        //        // Thread thread = new Thread(() => download(filename));

        //        // This needs to be much better handled, and the trigger queue should not trigger if a thread is running... 
        //        //Thread runPathThread = new Thread(() => RunPath(path));  // not working for some reason...
        //        //runPathThread.Start();

        //        pathExecuter = new Thread(() => RunPath(path));  // http://stackoverflow.com/a/3360582
        //        pathExecuter.Start();
        //    }
        //}

        ///// <summary>
        ///// Generates a module from a path, loads it to the controller and runs it.
        ///// It assumes the robot is stopped (does this even matter anyway...?)
        ///// </summary>
        ///// <param name="path"></param>
        //public void RunPath(Path path)
        //{
        //    Console.WriteLine("RUNNING NEW PATH: " + path.Count);
        //    List<string> module = Compiler.UNSAFEModuleFromPath(path, currentSettings.Speed, currentSettings.Zone);

        //    comm.LoadProgramToController(module);
        //    comm.StartProgramExecution();
        //}

        ///// <summary>
        ///// Remove all pending elements from the queue.
        ///// </summary>
        //public void ClearQueue()
        //{
        //    queue.EmptyQueue();
        //}

        ///// <summary>
        ///// Adds a Frame to the streaming queue
        ///// </summary>
        ///// <param name="frame"></param>
        //public void AddFrameToStreamQueue(Frame frame)
        //{
        //    streamQueue.Add(frame);
        //}

        //// This should be moved somewhere else
        //public static bool IsBelowTable(double z)
        //{
        //    return z < SAFETY_TABLE_Z_LIMIT;
        //}









        //  ██████╗ ███████╗██████╗ ██╗   ██╗ ██████╗ 
        //  ██╔══██╗██╔════╝██╔══██╗██║   ██║██╔════╝ 
        //  ██║  ██║█████╗  ██████╔╝██║   ██║██║  ███╗
        //  ██║  ██║██╔══╝  ██╔══██╗██║   ██║██║   ██║
        //  ██████╔╝███████╗██████╔╝╚██████╔╝╚██████╔╝
        //  ╚═════╝ ╚══════╝╚═════╝  ╚═════╝  ╚═════╝ 
        //                                            
        public void DebugDump()
        {
            DebugBanner();
            _driver.DebugDump();
        }

        public void DebugBuffers()
        {
            logger.Debug("VIRTUAL BUFFER:");
            IssueCursor.LogBufferedActions();

            logger.Debug("WRITE BUFFER:");
            ReleaseCursor.LogBufferedActions();

            logger.Debug("MOTION BUFFER");
            ExecutionCursor.LogBufferedActions();
        }

        public void DebugRobotCursors()
        {
            if (IssueCursor == null)
                logger.Debug("Virtual cursor not initialized");
            else
                logger.Debug(IssueCursor);

            if (ReleaseCursor == null)
                logger.Debug("Write cursor not initialized");
            else
                logger.Debug(ReleaseCursor);

            if (ExecutionCursor == null)
                logger.Debug("Motion cursor not initialized");
            else
                logger.Debug(ReleaseCursor);
        }

        //public void DebugSettingsBuffer()
        //{
        //    settingsBuffer.LogBuffer();
        //    Console.WriteLine("Current settings: " + currentSettings);
        //}

        /// <summary>
        /// Printlines a "DEBUG" ASCII banner... ;)
        /// </summary>
        private void DebugBanner()
        {
            logger.Debug("");
            logger.Debug("██████╗ ███████╗██████╗ ██╗   ██╗ ██████╗ ");
            logger.Debug("██╔══██╗██╔════╝██╔══██╗██║   ██║██╔════╝ ");
            logger.Debug("██║  ██║█████╗  ██████╔╝██║   ██║██║  ███╗");
            logger.Debug("██║  ██║██╔══╝  ██╔══██╗██║   ██║██║   ██║");
            logger.Debug("██████╔╝███████╗██████╔╝╚██████╔╝╚██████╔╝");
            logger.Debug("╚═════╝ ╚══════╝╚═════╝  ╚═════╝  ╚═════╝ ");
            logger.Debug("");
        }






        //  ███████╗██╗   ██╗███████╗███╗   ██╗████████╗███████╗
        //  ██╔════╝██║   ██║██╔════╝████╗  ██║╚══██╔══╝██╔════╝
        //  █████╗  ██║   ██║█████╗  ██╔██╗ ██║   ██║   ███████╗
        //  ██╔══╝  ╚██╗ ██╔╝██╔══╝  ██║╚██╗██║   ██║   ╚════██║
        //  ███████╗ ╚████╔╝ ███████╗██║ ╚████║   ██║   ███████║
        //  ╚══════╝  ╚═══╝  ╚══════╝╚═╝  ╚═══╝   ╚═╝   ╚══════╝
        //    
        /// <summary>
        /// Use this to trigger an `ActionIssued` event.
        /// </summary>
        internal void RaiseActionIssuedEvent()
        {
            Action lastAction = this.IssueCursor.GetLastAction();

            ActionIssuedArgs args = new ActionIssuedArgs(lastAction, this.GetCurrentPosition(), this.GetCurrentRotation(), this.GetCurrentAxes(), this.GetCurrentExternalAxes());

            this.parentRobot.OnActionIssued(args);
        }

        /// <summary>
        /// Use this to trigger an `ActionReleased` event.
        /// </summary>
        internal void RaiseActionReleasedEvent()
        {
            Action lastAction = this.ReleaseCursor.GetLastAction();
            int pendingRelease = this.ReleaseCursor.ActionsPendingCount();

            ActionReleasedArgs args = new ActionReleasedArgs(lastAction, pendingRelease, this.GetCurrentPosition(), this.GetCurrentRotation(), this.GetCurrentAxes(), this.GetCurrentExternalAxes());

            this.parentRobot.OnActionReleased(args);
        }

        /// <summary>
        /// Use this to trigger an `ActionExecuted` event.
        /// </summary>
        internal void RaiseActionExecutedEvent()
        {
            Action lastAction = this.ExecutionCursor.GetLastAction();
            int pendingExecutionOnDevice = this.ExecutionCursor.ActionsPendingCount();
            int pendingExecutionTotal = this.ReleaseCursor.ActionsPendingCount() + pendingExecutionOnDevice;

            ActionExecutedArgs args = new ActionExecutedArgs(lastAction, pendingExecutionOnDevice, pendingExecutionTotal, this.GetCurrentPosition(), this.GetCurrentRotation(), this.GetCurrentAxes(), this.GetCurrentExternalAxes());

            this.parentRobot.OnActionExecuted(args);
        }

        /// <summary>
        /// Use this to trigger a `MotionUpdate` event.
        /// </summary>
        internal void RaiseMotionUpdateEvent()
        {
            MotionUpdateArgs args = new MotionUpdateArgs(this.MotionCursor.position, this.MotionCursor.rotation, this.MotionCursor.axes, this.MotionCursor.externalAxesCartesian);

            this.parentRobot.OnMotionUpdate(args);
        }


    }
}
