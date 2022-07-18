# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [7.0.1] - 2022-02-14

### Fixed

- Fixed exception being thrown because of destroyed bricks being still considered as enabled during recomputation of the hierarchy

## [7.0.0] - 2022-01-28

### Fixed

- Fixed crash bug when duplicating bricks
- Improved prefab handling on auto update hierarchy
- Fixed issue where all undo groups in a session were accidentally collapsed
- General improvements and fixes for auto update hierarchy
- Fixed for knobs and tubes not being shown in all cases of disconnecting bricks
- Fixed a bug where selection state was wrong after duplicating a brick
- Fixed rotating bricks interaction where rotating right and left would rotate the same direction
- Fixed issue where objects were sometimes created outside of the prefab stage with auto update hierarchy
- Fixed issue where duplicating a brick selected through the sceneview in the hierarchy would break brick building

### Added

- Added warning to prevent splitting a modelgroup if it is the prefab root in the prefab stage

- Added TransformTracker that can be used through the `SceneBrickBuilder` at editor time to track changed transforms. 
- Added editor event  `bricksMoved` in `SceneBrickBuilder` you can subscribe to if you want to know if bricks were nudged or rotated
- Added editor event `startBrickMove` in `SceneBrickBuilder` you can subscribe to if you want to know when a brick move has started during building
- Added editor event `bricksPlaced` in `SceneBrickBuilder` you can subscribe to if you want to know when bricks are placed during building
- Added public editor time accessor for bricks in the scene through `SceneBrickBuilder`

### Changed

- Changed `BrickBuildingUtility.Connect` API to optionally record undo editor time
- Changed `MathUtils.AlignRotation` functions to take either one or two source axes instead of a variable length `params` to prevent unnecessary garbage collection

## [6.0.0] - 2021-11-18

### Fixed
- Placement of a number of bricks
- Pivot computation on model reimport
- Missing LEGO logo on processed models

## [5.0.0] - 2021-04-08

### Added
- Axle connectivity
- Fixed connectivity

### Fixed
- Placement of a number of bricks
- Pivot computation on model reimport

## [4.0.0] - 2021-03-16

### Changed
- Update package for 2020 LTS

### Fixed
- Various bugfixes

## [3.2.0] - 2021-01-18

### Added
- Ability to show and hide bricks in hierarchy

### Fixed
- Various bugs

## [3.0.3] - 2020-11-25

### Added
- Support for more brick connections

### Changed
- Model pivots are now updated when brick building
- Connectivity performance optimizations
- Update Materials package to 2.1.0

### Fixed
- Various bugs

## [3.0.2] - 2020-10-28

### Fixed
- Fixed issue with connectivity feature layer check.

## [3.0.1] - 2020-10-23

### Fixed
- Fixed issue with build time due to marking prefabs as dirty when they aren't.

## [3.0.0] - 2020-10-21

### Changed
- Improved model importer

### Added
- Added UI for brick rotation
- Added LEGO model protection
- Added automatic hierarchy update when building with bricks

### Fixed
- Various bug fixes

## [2.1.0] - 2020-08-27

### Added
- Added LEGO asset protection

### Changed
- Changed included LEGO model

## [2.0.1] - 2020-08-26

### Changed
- Update LEGO ® Tools UI
- Update package description

## [2.0.0] - 2020-08-25

### Removed
- Remove the SharpZipLib Dependency

### Changed
- Updated Brick Database (LegacyParts.zip & NewParts.zip)
- Changed naming of menu items

## [1.0.0] - 2020-08-20

### Added
- LEGO Model Importer Package v1.0.0 Release
