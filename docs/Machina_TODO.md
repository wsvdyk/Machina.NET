```text
//  ███╗   ███╗ █████╗  ██████╗██╗  ██╗██╗███╗   ██╗ █████╗ 
//  ████╗ ████║██╔══██╗██╔════╝██║  ██║██║████╗  ██║██╔══██╗
//  ██╔████╔██║███████║██║     ███████║██║██╔██╗ ██║███████║
//  ██║╚██╔╝██║██╔══██║██║     ██╔══██║██║██║╚██╗██║██╔══██║
//  ██║ ╚═╝ ██║██║  ██║╚██████╗██║  ██║██║██║ ╚████║██║  ██║
//  ╚═╝     ╚═╝╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝
//                                                          
//  ████████╗ ██████╗ ██████╗  ██████╗                      
//  ╚══██╔══╝██╔═══██╗██╔══██╗██╔═══██╗                     
//     ██║   ██║   ██║██║  ██║██║   ██║                     
//     ██║   ██║   ██║██║  ██║██║   ██║                     
//     ██║   ╚██████╔╝██████╔╝╚██████╔╝                     
//     ╚═╝    ╚═════╝ ╚═════╝  ╚═════╝                      
//                                                          
```


## IMPROVEMENTS
- [ ] Cleanup dead code
- [ ] Add comments on all the new functions
- [ ] Verbose refactoring
- [ ] When the buffer is empty, raise an event.


---
## FOR 0.6.0 RELEASE
This release will focus on reworking the Streaming mode to base it off TCP connection with the controller server. 

- [x] New factory constructor: `new Robot(...)` is now `Robot.Create(...)`
- [x] `Offline` mode working with new architecture
- [x] Add `ConnectionManager()` with options `user` (they are in charge of setting up communication server/firmatas) or `machina` (the library will try to make its best to figure out connection).
- [x] Fix C:/mod permissions for non-admins, use ENV system path
- [x] Rework the ABB real-time connection
- [x] Add an overload for TransformTo that takes single values as x, y, z, xvec0, xvec1, xvec2, yvec0, yvec1, yvec2. 

### Machina_Driver.mod
[x] Cleanup dead code
[x] Refactor function names and variables
[ ] Make parsing variables global for performance... (?)
[x] Add circular logic to the actions buffer: when written more than 1000, start again
[x] Add support for all Machina actions
[x] Implement more robust connect/disconnect logic, like in Robo_DK
[x] Implement ack messages, tell the client what's happening
[x] Add optional ids to the messages
[ ] ~~Add `Stream\Execute` modes: actions are executed immediately upon reception or buffered until instructed to perform an execution~~ → Leave for later
[x] Add a handshake of versions and ips and stuff
[x] Add `USE_STRICT` flag to exit execution on anything going wrong

- [ ] `Precision()` should accept `double` input


---
## FOR 0.5.0 RELEASE
- [x] Add `Temperature()`
- [x] Add `Extrude()`
- [x] Add `ExtrusionRate()`
- [x] Test 'Temperature' etc
- [x] Implement GCode compiler for ZMorph
- [x] Basic 3D Printing example
- [x] Make sure Extrusion Actions don't cause weird effects in non-3D printer compilers and viceversa

- [x] Rethink what the 3D printer does automatically and what needs to be managed by the user: temperature, calibration, homing... --> The philosophy of the library is that it is a very low-level 3D printer interface as a result of the ibject being a machine that can move in 3D space. It is for simple custom operations, not really for hi-end printing (user would be much better off using a slicer software). 
    - [x] Focus on the ZMorph for now; if at some point I use other printer, will expand functionality.
    - [x] Add `Initialize()` and `Terminate()` for custom initialization and ending boilerplates.
    - [ ] ~~Change `Extrude(bool)` to `Extrude(double)` to include ExtrusionRate, and remove `ExtrusionRate`~~ --> let's keep it like this for the moment, might be confusing/tyring to combine them. --> Perhaps add a `Extrude(double)` overload tht combines them both?

- [ ] ~~REMOVE THE REGULAR/TO MODEL, and add a ActionMode("absolute"/"relative") to substitute it!~~
    - ActionMode becomes a property of the cursor.
    - Under this, Actions cannot be independently defined, but their meaning varies depending on when/where in the program they have been issued! :( This detracts from the conceptual independence of the Action and its platform-agnosticity... This may make sense in command-line environments, but will be quite shitty in VPLs
    - Make `Push/PopSettings()` store `ActionMode` too?
--> Decided not to go for this. The focus of this project is the CORE library, not the VPLs APIS... And when writting Machina code, the ...To() suffix is quite convenient and literal to quickly switch between modes, makes different explicit, is faster to type/read, and works better with auto completion in dev IDEs. Furtehrmore, it is interesting to keep the idea that Actions are agnostic to the medium; it would be weird if the same line of code would mean different things depending on the state of the cursor: the action should be absolute or relative on its own. 

- [x] Add `ExtrusionRateTo()` and `TemperatureTo()`
- [x] Rename "MotionType" to "MotionMode": action API and enum value
- [x] Rename `Mode()` to `ControlMode()`
- [x] Rnemae `RunMode()` to `CycleMode()`
- [ ] ~~Rename '`Attach`' to '`AttachTool`', and '`Detach`' to '`DetachTools`'...?~~
- [ ] ~~Rename '`PushSettings`' to '`SettingsPush`' and same for `Pop`?~~
- [x] Print a disclaimer header for exported code 
    - [x] Fix ASCII art --> It is bad when writting text from UTF-8 to ASCII (every filetype but human...)
- [x] Rename `Zone` and `Joints` Actions in actions
- [x] Fix OfflineAPIs

- [x] Make components have the option to choose between abs/rel
- [x] Rename 'Motion' to 'MotionType' here and Dyn (GH is changed)
- [x] Rename `FeedRate` to `ExtrusionRate` in DYN+GH
- [x] Remove all obsolete components, and create new ones with GUID to avoid overwrite
- [x] Update Dyn+GH in general
- [x] Wrap up Icons
- [x] Redo DYN+GH sample files


## AL·GO MEETING
- [x] HUMAN mode not working...
- [ ] Add a json mode? Any form of de/serializable format that can become "Machina" code, like a Machina save format, that could be loaded and executed, also exchanged between apps
- [ ] Export doesn' flush automatically in oflline mode, this should be made explicit with a .ClearMemory() function or similar...


## LATER..
- [ ] Add enhanced CoordSys selection and WObj use

- [ ] Verify program names and IOnames cannot start with a digit
- [ ] Add Acceleration
- [ ] Add Action constructors that take atomic primitives (x,y,z instead of a point object): they shallow copy it anyway... optimization
    - [ ] Use these constructors in Dynamo
    - [ ] Use these constructors in Grasshopper
- [ ] Are the Action... static constructors still necessary?
- [ ] Add 'null' checks for Action lists before compile (Dynamo + GH?)
- [ ] Remove TurnOn/Off

- [ ] Create `Program` as a class that contains a list of actions? It could be interesting as a way to enforce the idea of Programs as a list of Actions, especially in VPL interfaces. Also, it would allow to do things such as adding an `Instruction` (like a function) to the scope of a program, that could be called from the Program itself.

- [ ] Create `Combine` action where two or more Actions are combined into a single one. Useful for example for Move+DO actions, Temp+Wait actions, Move+Rotate (=Transform), or any other combination allowed by the compiler...? -

- [ ] `Execute` mode should have some way of signaling if the uploaded program finished, like an event or something.

- [ ] If on online mode, if connection is lost with the controller, the app throws an error. Implement softreset

- [ ] Improve the AxesTo -> TransformTo message, make it understandable... 

- [ ] Add a .Home() function?


----
# PHASE 2

## HIGH-LEVEL
- [x] RENAME THE PROJECT!
- [x] Add support for KUKA + UR compilation
- [x] Add Tools 
- [x] Improve BRobot for Dynamo, create package, publish
- [x] Deactivate action# display, and replace with the human version?
- [x] Rename Zone to Precision
- [x] Redo github banner
- [x] Post links to some Machina videos on YouTube
- [x] Create Grasshopper library





## LOW-LEVEL
- [ ] Rotation problem: the following R construction returns a CS system with the Y inverted!:
    ```csharp
        > Rotation r = new Rotation(-1, 0, 0, 0, 0, -1);
        > r
        [[0,0,0.70710678,0.70710678]]
        > r.GetCoordinateSystem()
        [[[-1,0,0],[0,0,1],[0,1,0]]]  // --> Notice the inverted Y axis!

        // It doesn't happen with this one for example:
        > Rotation r1 = new Rotation(-1, 0, 0, 0, 1, 0);
        > r1
        [[0,0,1,0]]
        > r1.GetCoordinateSystem()
        [[[-1,0,0],[0,1,0],[0,0,-1]]]
    
        // Another way to see this is the following:
        > CoordinateSystem cs = new CoordinateSystem(-1, 0, 0, 0, 0, -1);
        > cs
        [[[-1,0,0],[0,0,-1],[0,-1,0]]]
        > 
        > cs.GetQuaternion().GetCoordinateSystem()
        [[[-1,0,0],[0,0,1],[0,1,0]]]  // It is inverted!

        // However, this works well:
        > Rotation r = new Rotation(-1, 0, 0, 0, 1, -1);
        > r
        [[0,0,0.92387953,0.38268343]]
        > r.GetCoordinateSystem()
        [[[-1,0,0],[0,0.70710678,0.70710678],[0,0.70710678,-0.70710678]]]

        > Rotation r2 = new Rotation(-1, 0, 0, 0, 1, 0);
        > r2.GetCoordinateSystem()
        [[[-1,0,0],[0,1,0],[0,0,-1]]]
        > r2.RotateLocal(new Rotation(1, 0, 0, 45));
        > r2
        [[0,0,0.92387953,-0.38268343]]
        > r2.GetCoordinateSystem()
        [[[-1,0,0],[0,0.70710678,-0.70710678],[0,-0.70710678,-0.70710678]]]
        > r2.RotateLocal(new Rotation(1, 0, 0, 45));
        > r2.GetCoordinateSystem()
        [[[-1,0,0],[0,0,-1],[0,-1,0]]]
        > 
        // Maybe the problem is in the initial Vectors to Quaternion conversion?
        
    ```
    --> check https://github.com/westphae/quaternion/blob/master/quaternion.go ?
    --> Write some unit tests and test the library to figure this out
    --> Is this a problem inherent to Quaternion to Axis-Angle convertion, and the fact that the latter always returns positive rotations? 
- [ ] The dependency tree makes BRobot not work right now if the user doesn't have RobotStudio in the system. And the library is pretty much only used for comm, not used at all for offlien code generation. A way to figure this out, and have the library work in offline mode without the libraries should be implemented. 
- [ ] Coordinates(): this should:
    - [ ] Accept a CS object to use as a new reference frame
    - [ ] Use workobjects when compiled
    - [ ] Perform coordinate/orientation transforms when changing from one CS to the next
- [ ] Add `.Motion("circle")` for `movec` commands?
- [ ] Add `.Acceleration()` and `.AccelerationTo()` for UR robots?
- [ ] Rethink API names to be more 'generic' and less 'ABBish'
- [ ] This happens, should it be fixed...?
    ```csharp
    arm.Rotate(1, 0, 0, 225);  // interesting (and obvious): because internally this only adds a new target, the result is the robot getting there in the shortest way possible (performing a -135deg rotation) rather than the actual 225 rotation over X as would intuitively come from reading he API...
    ```
- [ ] UR simulator is doing weird things with linear vs. joint movements... --> RoboDK doesn't do it, but follows a different path on `movej`...
- [ ] Named CSs could be "world", "base", "flange", and "tool"/"tcp"
- [ ] Add `bot.Units("meters");` to set which units are used from a point on.
- [ ] Compilers are starting to look pretty similar. Abstract them into the superclass, and only override instruction-specific string generation?
- [ ] In KRL, user may export the file with a different name from the module... how do we fix this?
- [ ] Apparently, messages in KRL are kind fo tricky, with several manuals just dedicated to it. Figure this out.

- [ ] ROTATION REWORK:
    - [x] Create individual classes for AxisAngle (main), Quaternion, RotationVector (UR), Matrix33, and EulerZYX
    - [x] Create constructors for each one. 
    - [ ] Create conversions between them:
        * [ ] AxisAngle -> Quaternion
        * [ ] Quaternion -> AxisAngle
    - [ ]   


---

# PHASE 1

## HIGH-LEVEL
- [x] Merged ConnectionMode & OnlineMode into ControlMode
- [x] Restructured library
- [x] Redesigned API
- [x] Abstracted TCPPOsition, TCPRotation and stuff into a VirtualRobot object
- [x] Ported Util methods as static to their appropriate geometry class 
- [x] Rename the project to BRobot ;)
- [x] All the connection properties, runmodes and stuff should belong to the VirtualRobot?
- [ ] Created Debug() & Error() utility functions
- [ ] Detect out of position and joint errors and incorporate a soft-restart.
- [ ] Make changes in ControlMode at runtime possible, i.e. resetting controllers and communication, flushing queues, etc.
- [ ] Streamline 'bookmarked' positions with a dictionary in Control or similar 
- [ ] Implement .PointAt() (as in 'look' at somewhere)
- [ ] Rename Point to Vector (or add an empty subclass of sorts)
- [ ] Split off Quaternion from Rotation? API-wise, make the difference more explicit between a Rotation and a Quaternion...?



## LOW-LEVEL
- [ ] Low-level methods in Communication should not check for !isConnected, but rather just the object they need to perform their function. Only high-level functions should operate based on connection status.
- [ ] Fuse Path and Frame Queue into the same thing --> Rethink the role of the Queue manager
- [ ] Clarify the role of queue manager andits relation to Control and Comm.
- [ ] Unsuscribe from controller events on .Disconnect()



## FUTURE WISHLIST
- [ ] Bring back 'bookmarked' absolute positions.

## SOMETIME...
- [ ] Get my self a nice cold beer and a pat on the back... ;)


## DONE

