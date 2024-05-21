import cv2
import numpy as np
import pickle
import pandas as pd
from ultralytics import YOLO
import cvzone

def parkingLot_detect_cam(cam_index,config_file_path):
    with open(config_file_path,"rb") as f:
        data = pickle.load(f)
        polylines,area_names=data['polylines'],data['area_names']

    with open("coco.txt", "r") as my_file:
        data = my_file.read()
        class_list = data.split("\n")

    model=YOLO('yolov8s.pt')

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
        px=pd.DataFrame(a).astype("float")
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

    with open("coco.txt", "r") as my_file:
        data = my_file.read()
        class_list = data.split("\n")

    model=YOLO('yolov8s.pt')

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
        if count % 2 != 0:
            continue

        results=model.predict(frame)
        a=results[0].boxes.data
        px=pd.DataFrame(a).astype("float")
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