from pymongo import MongoClient
from AWSService import AwsParameterManager
from gridfs import GridFS
from PyQt5.QtMultimedia import QCameraInfo
from yolo import start_detect_cam
from concurrent.futures import ThreadPoolExecutor
from multiprocessing import freeze_support


if __name__ == '__main__':
    freeze_support()
    try:
        client = MongoClient('localhost', 27017)
        db = client.ParkEase
        parameterManager = AwsParameterManager()
        onlineClient = MongoClient(parameterManager.get_parameters('/ParkEase/Configs/ConnectionString'))
        onlineDb = onlineClient[parameterManager.get_parameters('/ParkEase/Configs/DatabaseName')]
        configGridFs = GridFS(db)
        camConfig = db.CamConfig
        camList = QCameraInfo.availableCameras()
        deviceNames = [camInfo.deviceName() for camInfo in camList]
        camConfigList = camConfig.find()
       
        with ThreadPoolExecutor() as executor:
            futures = []
            for camConfig in camConfigList:
                camName = camConfig.get("name")
                camDeviceName = camConfig.get("camDeviceName")
                camIndex = deviceNames.index(camDeviceName)
                futures.append(executor.submit(start_detect_cam, camIndex, camName))
        input()

    except Exception as e:
        print(f"Error occurred while starting the camera detection.{e}")
        input('\nPress key to exit.') 
