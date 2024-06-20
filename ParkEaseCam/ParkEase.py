from pymongo import MongoClient
from AWSService import AwsParameterManager
import gridfs
from PyQt5.QtMultimedia import QCameraInfo
from yolo import start_detect_cam
import concurrent.futures

client = MongoClient('localhost', 27017)
db = client.ParkEase
parameterManager = AwsParameterManager()
onlineClient = MongoClient(parameterManager.get_parameters('/ParkEase/Configs/ConnectionString'))
onlineDb = onlineClient[parameterManager.get_parameters('/ParkEase/Configs/DatabaseName')]
configGridFs = gridfs.GridFS(db)
camConfig = db.CamConfig
camList = QCameraInfo.availableCameras()
deviceNames = [camInfo.deviceName() for camInfo in camList]
if __name__ == '__main__':
    camConfigList = camConfig.find()
    with concurrent.futures.ThreadPoolExecutor() as executor:
        futures = []
        for camConfig in camConfigList:
            camName = camConfig.get("name")
            camDeviceName = camConfig.get("camDeviceName")
            camIndex = deviceNames.index(camDeviceName)
            #start_detect_cam(camIndex, camName)
            futures.append(executor.submit(start_detect_cam, camIndex, camName))