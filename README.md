# Diagnostics Module
This module used device streams to pull diagnostics data from an Anure IoT Edge device. It currently supports copying dump files to a developers machine using a command line interface.

## Usage
### Module
Deploy the diagnostics module to the device. The module has 2 required and 2 optional environment variables.
* ```INCOMING_DIRECTORY``` (r): Location other modules will leave their dumps. This should be a shared volume.
* ```STORAGE_DIRECTORY``` (r): The location dumps will be stored. The CLI will copy files from here.
* ```MAX_DISK_BYTES```: Maximum total file size that will be stored in ```STORAGE_DIRECTORY```. Default infinity.
* ```MAX_DISK_PERCENT```: Maximum percentage of disk used by stored files. Default ```50```.

[Example deployment manifest](docs\example_deployment.amd64.json)

### CLI
Once the module is deployed, the command line can be used with the following commands.
* ```ls```: list files avaliable to copy
* ```copyfile -s <targetFileName> -d <destinationFileName>```: copys \<targetFileName\> from module to developer's computer

## Model
![model](docs/DiagnosticsModule.png "Diagnostics Module"  )