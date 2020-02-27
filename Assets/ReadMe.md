
Book of the Dead: Environment
=============================

This package contains an extended version of the environment that is shown in our Book of the Dead trailer. It includes some of the locations used in the trailer, as well as some new ones. 

Built like a video game level, this project will give you insight into how to build that kind of detailed environment using HDRP and photogrammetry assets. The project features a first person player controller allowing the user to walk around in the environment.

All art assets in this package - including the Quixel Megascans - are governed by the standard Unity Asset Store EULA. The customized versions of Post-processing, Render Pipeline Core and High-Definition Render Pipeline included with the project are governed by the Unity Companion License. See ThirdPartyNotices.txt inside the package for detailed licensing information about each of the code components.



**

Instructions
============

This project requires Unity 2018.2f2 or higher. It should be imported into an empty project (created from '3D' template) to avoid conflicts with builtin packages. Please be aware that customized versions of some Unity packages (PostProcess/CoreRP/HDRP) are included in the download and might cause conflicts if the official versions of these same packages are referenced by the project manifest (leading to code compilation errors and other tomfoolery).


Loading Book of the Dead: Environment
-------------------------------------

Use the menu ‘Tools > Load Book of the Dead: Environment’ to load all the scenes of the project.


Loading the asset library
-------------------------

Use the menu ‘Tools > Load Asset Library’ to discover the asset library.


Area volumes
------------

The Area Volumes are built on the core volume system offered by SRP and are very similar to the Post Process Volumes. Their function is to drive object properties depending on the position of the MainCamera object.
Several objects, including the Directional Light, the Atmospheric Scattering and the WindControl  have their properties driven by  Area Volumes, so if you want to change the current lighting setup for example, you will need to do that in the corresponding AreaVolume. Those Area Volumes objects are located in the main scene, under _SceneSettings > _AREASETTINGS, and have the suffix ‘_AV’.


Game view tracking
------------------

Since the lighting and atmospheric scattering setup depend on the Main Camera position, we needed a way to easily move the MainCamera. By toggling the Game View Tracking in the menu Tools > Forest > Game View Tracking (Or by using the CTRL+T shortcut), the Main Camera will always follow the Scene View camera. Note: You will need to disable this tool to be able to scrub the Camera Flythrough Timeline.


Fly-through camera
------------------

A Fly-through camera mode is available in play mode, you can toggle it using the TAB key on a keyboard, or the UP d-pad button on a controller. The animation of this fly-through camera is saved in the timeline of the object ‘CameraPath_Timeline’ in the project. Select it with the Timeline window open, and you will be able to scrub through the camera path animation.


Game mode controls
------------------

When in play mode, press the ‘Escape’ key, the ‘Start/Menu’ on your Xbox controller or ‘Options’ on your PS4 controller to see all the controls available. You can look around, walk, run, jump, enter the fly-through camera mode and scrub backward/forward in this mode.


Supported platforms
-------------------

This project supports building standalone players for the following platforms:
- Windows DX11/DX12/Vulkan
- macOS/Metal
- PS4/PS4 Pro
- Xbox One/Xbox One S/Xbox One X 


**

The package includes
====================

- Environment art assets scanned by Quixel, coming either from the Megascans library or produced exclusively for this demo.
- Environment art assets produced by the Demo team using photogrammetry.
- Environment scene, setup using the HD Render Pipeline.
- Player Controller and a Camera fly-through mode to discover the scene in play mode
- Area Volumes: Volume based system driving atmospheric scattering, sunlight and wind properties.
- Custom atmospheric scattering.
- Custom vertex shader for procedurally animated wind, used by all our vegetation.
- Project-specific Lit shader customizations.
- Project-specific customizations to lighting, shadows and occlusion inputs and calculations.
- Occlusion Probes, a baked solution for efficient sky occlusion on foliage.
- Grass occlusion system to create additional occlusion for our smaller vegetation assets placed on the terrain. 
- Sound effects, and a fully functional audio landscape

