import cv2
import pickle
from pandas import DataFrame
from ultralytics import YOLO
import cvzone
from gridfs import GridFS
from pymongo import MongoClient
from AWSService import AwsParameterManager
from datetime import datetime
import threading
import os
import sys

stopSignal = False
parameterManager = AwsParameterManager()
onlineClient = MongoClient(parameterManager.get_parameters('/ParkEase/Configs/ConnectionString'))
onlineDb = onlineClient[parameterManager.get_parameters('/ParkEase/Configs/DatabaseName')]
client = MongoClient('localhost', 27017)
localDb = client.ParkEase

#https://stackoverflow.com/questions/31836104/pyinstaller-and-onefile-how-to-include-an-image-in-the-exe-file
def resource_path(relative_path):
    try:
        base_path = sys._MEIPASS
    except Exception:
        base_path = os.path.abspath(".")

    return os.path.join(base_path, relative_path)

def stopTesting():
    global stopSignal
    stopSignal = True
def parkingLot_detect_cam(cam_index,config_data):
    polylines,area_names=config_data['polylines'],config_data['area_names']

    with open(resource_path("coco.txt"), "r") as my_file:
        data = my_file.read()
        class_list = data.split("\n")

    model=YOLO(resource_path('yolov8s.pt'))

    cap=cv2.VideoCapture(cam_index, cv2.CAP_DSHOW)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 960)
    count=0

    while True:
        ret, frame = cap.read()
        if not ret:
            cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            continue

        count += 1
        if count % 2 != 0:
            continue

        results=model.predict(frame)
        a=results[0].boxes.data
        px=DataFrame(a).astype("float")
        carPosition =[]
        #detecting cars
        for index,row in px.iterrows():
            x1=int(row[0])
            y1=int(row[1])
            x2=int(row[2])
            y2=int(row[3])
            d=int(row[5])

            c=class_list[d]
            cx=int(x1+x2)//2
            cy=int(y1+y2)//2
            if 'car' in c:
                carPosition.append((cx,cy))

        #drawing polylines
        for i,polyline in enumerate(polylines):
            carInside = False

            #checking if car is inside the polyline
            for carP in carPosition:
                cx1= carP[0]
                cy1= carP[1]

                result=cv2.pointPolygonTest(polyline,(cx1,cy1),False)
                if result >= 0:
                    carInside = True
                    cv2.polylines(frame,[polyline],True,(0,0,255),2)
                    break

            #if car is not inside the polyline, draw the polyline in green
            if carInside == False:
                cv2.polylines(frame,[polyline],True,(0,255,0),2)
            cvzone.putTextRect(frame,f'{area_names[i]}',tuple(polyline[0]),1,1)

        cv2.imshow('FRAME', frame)
        key = cv2.waitKey(50) & 0xFF
        if key == 27:
            break
    cap.release()
    cv2.destroyAllWindows()
            
def parkingLot_detect_video(video_filePath,config_file_path):
    with open(config_file_path,"rb") as f:
        data = pickle.load(f)
        polylines,area_names=data['polylines'],data['area_names']

    with open(resource_path("coco.txt"), "r") as my_file:
        data = my_file.read()
        class_list = data.split("\n")

    model=YOLO(resource_path('yolov8s.pt'))

    cap=cv2.VideoCapture(video_filePath)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 960)

    count=0

    while True:
        ret, frame = cap.read()
        if not ret:
            cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            continue
        count += 1
        if count % 3 != 0:
            continue

        results=model.predict(frame)
        a=results[0].boxes.data
        px=DataFrame(a).astype("float")
        carPosition =[]
        #detecting cars
        for index,row in px.iterrows():
            x1=int(row[0])
            y1=int(row[1])
            x2=int(row[2])
            y2=int(row[3])
            d=int(row[5])

            c=class_list[d]
            cx=int(x1+x2)//2
            cy=int(y1+y2)//2
            if 'car' in c:
                carPosition.append((cx,cy))

        #drawing polylines
        for i,polyline in enumerate(polylines):
            carInside = False

            #checking if car is inside the polyline
            for carP in carPosition:
                cx1 = carP[0]
                cy1 = carP[1]

                result=cv2.pointPolygonTest(polyline,(cx1,cy1),False)
                if result >= 0:
                    carInside = True
                    cv2.polylines(frame,[polyline],True,(0,0,255),2)
                    break

            #if car is not inside the polyline, draw the polyline in green
            if carInside == False:
                cv2.polylines(frame,[polyline],True,(0,255,0),2)
            cvzone.putTextRect(frame,f'{area_names[i]}',tuple(polyline[0]),1,1)

        cv2.imshow('FRAME', frame)
        key = cv2.waitKey(50) & 0xFF
        if key == 27:
            break
    cap.release()
    cv2.destroyAllWindows()

def start_detect_cam(camIndex,configName):
    CamConfig = localDb.CamConfig
    ConfigGridFs = GridFS(localDb)
    area_config = CamConfig.find_one({"name":configName})
    data = ConfigGridFs.find_one({"filename": configName})
    cam_config = pickle.loads(data.read())
    if(area_config is None or cam_config is None):
        print("Config not found")
        return 

    areaType = area_config.get('areaType')
    if(areaType == 'Public'):
        start_detect_cam_public(camIndex,area_config,cam_config)
    else:
        # get the floor from spliting the configName by '_' in the last element
        floor = configName.split('_')[-1]
        start_detect_cam_private(camIndex,area_config,cam_config,floor)

def start_detect_cam_public(cam_index,area_config,cam_config):
    logDC = onlineDb.PublicLog
    statusDC = onlineDb.PublicStatus

    areaId = area_config.get("areaId")
    cam_name = area_config.get("displayName")
    query = {"areaId": areaId,"camName": cam_name}
    status = statusDC.find(query)

    local_status = {s['index']: s['status'] for s in status}
    polylines = cam_config['polylines']
    with open(resource_path("coco.txt"), "r") as my_file:
        data = my_file.read()
        class_list = data.split("\n")

    model=YOLO(resource_path('yolov8s.pt'))

    cap=cv2.VideoCapture(cam_index, cv2.CAP_DSHOW)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 960)
    print(f"Camera {cam_name}, Running on {threading.current_thread().name}")
    while True:
        ret, frame = cap.read()
        if not ret:
            cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            continue

        results=model.predict(frame,verbose=False)
        a=results[0].boxes.data
        px=DataFrame(a).astype("float")
        carPosition =[]
        #detecting cars
        for index,row in px.iterrows():
            x1=int(row[0])
            y1=int(row[1])
            x2=int(row[2])
            y2=int(row[3])
            d=int(row[5])

            c=class_list[d]
            cx=int(x1+x2)//2
            cy=int(y1+y2)//2
            if 'car' in c:
                carPosition.append((cx,cy))

        #drawing polylines
        for i,polyline in enumerate(polylines):
            carInside = False

            #checking if car is inside the polyline
            for carP in carPosition:
                result=cv2.pointPolygonTest(polyline,(carP[0],carP[1]),False)
                #result: = 0 on edge, < 0 outside, > 0 inside
                if result > 0:
                    carInside = True
                    break
            dir = {
                    "areaId":areaId,
                    "index" : i,
                    "camName":cam_name,
                    "status" : carInside,
                    "timestamp": datetime.now()
                }
            #if car is inside the polyLine, update the status
            if i in local_status:
                if local_status[i] != carInside:
                    statusDC.update_one(dir, upsert=True)
                    logDC.insert_one(dir)
            else:
                statusDC.insert_one(dir)
                logDC.insert_one(dir)
            
            local_status[i] = carInside

        key = cv2.waitKey(50) & 0xFF
        if key == 27:
            break
    cap.release()

def start_detect_cam_private(cam_index,area_config,cam_config,floor):
    logDC = onlineDb.PrivateLog
    statusDC = onlineDb.PrivateStatus

    lotIds = area_config.get('lotIds')
    areaId = area_config.get("areaId")
    cam_name = area_config.get("displayName")
    query = {"areaId": areaId, "floor": floor,"camName": cam_name}
    status = statusDC.find(query)
    local_status = {s['index']: s['status'] for s in status}
    polylines = cam_config['polylines']

    with open(resource_path("coco.txt"), "r") as my_file:
        data = my_file.read()
        class_list = data.split("\n")

    model=YOLO(resource_path('yolov8s.pt'))

    cap=cv2.VideoCapture(cam_index, cv2.CAP_DSHOW)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 960)
    print(f"Camera {cam_name}, Running on {threading.current_thread().name}")
    while True:
        ret, frame = cap.read()
        if not ret:
            cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            continue

        results=model.predict(frame,verbose=False)
        a=results[0].boxes.data
        px=DataFrame(a).astype("float")
        carPosition =[]
        #detecting cars
        for index,row in px.iterrows():
            x1=int(row[0])
            y1=int(row[1])
            x2=int(row[2])
            y2=int(row[3])
            d=int(row[5])

            c=class_list[d]
            cx=int(x1+x2)//2
            cy=int(y1+y2)//2
            if 'car' in c:
                carPosition.append((cx,cy))

        #drawing polylines
        for i,polyline in enumerate(polylines):
            carInside = False

            #checking if car is inside the polyline
            for carP in carPosition:
                result=cv2.pointPolygonTest(polyline,(carP[0],carP[1]),False)
                #result: = 0 on edge, < 0 outside, > 0 inside
                if result > 0:
                    carInside = True
                    break
            dir = {
                    "areaId":areaId,
                    "index" : i,
                    "camName": cam_name,
                    "lotId" : lotIds[str(i+1)],
                    "status" : carInside,
                    "floor" : floor,
                    "timestamp": datetime.now()
                }
            if i in local_status:
                if local_status[i] != carInside:
                    statusDC.update_one({"index": i,"camName":cam_name}, {'$set':dir}, upsert=True)  
                    logDC.insert_one(dir)
            else:
                statusDC.insert_one(dir)
                logDC.insert_one(dir)

            local_status[i] = carInside

        key = cv2.waitKey(50) & 0xFF
        if key == 27:
            break
    cap.release()

def start_detect_video(video_filePath,config_file_path,areaType,id):
    global stopSignal
    videoTestDC = onlineDb.videoTest
    if areaType == 'Public':
        logDC = onlineDb.PublicLog
    else:
        logDC = onlineDb.PrivateLog
    status = videoTestDC.find({"areaId":id,"camName": "test"})
    local_status = {s['index']: s['status'] for s in status}
    with open(config_file_path,"rb") as f:
        data = pickle.load(f)
        polylines=data['polylines']

    with open(resource_path("coco.txt"), "r") as my_file:
        data = my_file.read()
        class_list = data.split("\n")
    
    model=YOLO(resource_path('yolov8s.pt'))

    cap=cv2.VideoCapture(video_filePath)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 960)
    print("Camera: test start video detection")
    while True:
        ret, frame = cap.read()
        if not ret:
            cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            continue
        results=model.predict(frame,verbose=False)
        a=results[0].boxes.data
        px=DataFrame(a).astype("float")
        carPosition =[]
        #detecting cars
        for index,row in px.iterrows():
            x1=int(row[0])
            y1=int(row[1])
            x2=int(row[2])
            y2=int(row[3])
            d=int(row[5])

            c=class_list[d]
            cx=int(x1+x2)//2
            cy=int(y1+y2)//2
            if 'car' in c:
                carPosition.append((cx,cy))

        #drawing polylines
        for i,polyline in enumerate(polylines):
            carInside = False

            #checking if car is inside the polyline
            for carP in carPosition:
                result=cv2.pointPolygonTest(polyline,(carP[0],carP[1]),False)
                #result: = 0 on edge, < 0 outside, > 0 inside
                if result > 0:
                    carInside = True
                    break
            if i in local_status:
                if local_status[i] != carInside:
                    if areaType == 'Public':
                        dir = {
                            "areaId":id,
                            "index" : i,
                            "camName": "test",
                            "status" : carInside
                        }
                        videoTestDC.update_one({"index": i,"camName": "test"}, {"$set": dir}, upsert=True)
                        logDC.insert_one(dir)    
                    else:
                        lotId = i + 1
                        dir = {
                            "areaId":id,
                            "index" : i,
                            "camName": "test",
                            "lotId" : lotId,
                            "status" : carInside,
                            "floor" : 'Ground',
                            "timestamp": datetime.now(),
                        }
                        videoTestDC.update_one({"index": i,"camName": "test"}, {'$set':dir}, upsert=True)
                        logDC.insert_one(dir)
                    local_status[i] = carInside    
            else:
                if areaType == 'Public':
                    dir = {
                            "areaId":id,
                            "index" : i,
                            "camName": "test",
                            "status" : carInside,
                            "timestamp": datetime.now()
                        }
                    videoTestDC.insert_one(dir)
                    logDC.insert_one(dir)
                else:
                    lotId = i + 1
                    dir = {
                        "areaId":id,
                        "index" : i,
                        "camName": "test",
                        "lotId" : lotId,
                        "status" : carInside,
                        "floor" : 'Ground',
                        "timestamp": datetime.now()
                    }
                    videoTestDC.insert_one(dir)
                    logDC.insert_one(dir)
                local_status[i] = carInside    

        if stopSignal == True:
            cv2.imwrite("TestLast.jpg", frame) 
            break
    
    cap.release()
    stopSignal = False