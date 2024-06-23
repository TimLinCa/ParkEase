#QtDesinger Reference: https://github.com/Phatthawat/OBJECT-COUNTING-MACHINE-USING-COMPUTER-VISION
#Parking lot Reference: https://www.youtube.com/watch?v=MyvylXVWYjY&t=3000s
#Polygon Reference: https://stackoverflow.com/questions/52751121/pyqt-user-editable-polygons
import subprocess
import cv2,time,sysinfo
from AWSService import AwsParameterManager
from PyQt5 import uic,QtCore,QtWidgets
from PyQt5.QtMultimedia import QCameraInfo
from PyQt5.QtWidgets import QApplication, QMainWindow,QMessageBox
from PyQt5.QtCore import QThread, pyqtSignal,QTimer, QDateTime
from PyQt5.QtGui import QImage, QPixmap
from Polygon import AnnotationView, Instructions, GripItem,PolygonAnnotation
import numpy as np
import gridfs
from functools import partial
import pickle
from yolo import parkingLot_detect_video,parkingLot_detect_cam,start_detect_video,stopTesting
from pymongo import MongoClient

client = MongoClient('localhost', 27017)
db = client.ParkEase
parameterManager = AwsParameterManager()
onlineClient = MongoClient(parameterManager.get_parameters('/ParkEase/Configs/ConnectionString'))
onlineDb = onlineClient[parameterManager.get_parameters('/ParkEase/Configs/DatabaseName')]

CamConfig = db.CamConfig
configGridFs = gridfs.GridFS(db)
publicArea = onlineDb.ParkingData
privateArea = onlineDb.PrivateParking

polyitems = []
area_names=[]
points = []
drawing = False
polygon_index = 1
config_id = None
camdevicetest = []
class MainWindow(QMainWindow):
    def __init__(self, *args, obj=None, **kwargs):
        super(MainWindow, self).__init__(*args, **kwargs)
        global IsDrawingMode, camIndex
        IsDrawingMode = False
        #super().__init__()
        # Load the UI
        self.ui = uic.loadUi("MainWindow.ui",self)
        self.Initialize()
        # Get the list of available cameras
        self.online_cam = QCameraInfo.availableCameras()
        self.camlist.addItems([c.description() for c in self.online_cam])

        self.cmb_areaType.addItems(["Public","Private"])
        self.btn_stop.setEnabled(False)

        #Set the radio button to default true
        self.rab_cam.setChecked(True)

        # Set the command for the buttons
        self.btn_start.clicked.connect(self.startWebCam)
        self.btn_stop.clicked.connect(self.stopWebcam)
        self.btn_draw.clicked.connect(self.drawMode)
        self.bnt_test.clicked.connect(self.detectionTest)
        self.bnt_saveconfig.clicked.connect(self.saveConfig)
        self.bnt_loadconfig.clicked.connect(self.loadConfig)
        self.bnt_importVideo.clicked.connect(self.importVideo)
        self.bnt_importTestConfig.clicked.connect(self.importTestConfig)
        self.bnt_loadArea.clicked.connect(self.loadPrivateArea)
        self.btn_bind.clicked.connect(self.bindCam)
        self.bnt_dbTest.clicked.connect(self.DbTest)
        # Set command for when combobox is changed
        self.cmb_areaType.currentIndexChanged.connect(self.areaTypeChanged)
        self.cmb_floor.currentIndexChanged.connect(self.floorChanged)
        self.bnt_loadArea.setEnabled(False)
        self.cmb_lotId.setEnabled(False)

        # Resource Usage
        self.resource_usage = boardInfoClass()
        self.resource_usage.start()
        self.resource_usage.cpu.connect(self.getCPU_usage)
        self.resource_usage.ram.connect(self.getRAM_usage)

        # Timer
        self.lcd_timer = QTimer()
        self.lcd_timer.timeout.connect(self.clock)
        self.lcd_timer.start()

        # Graphics View
        self.m_scene = AnnotationScene(self)
        self.m_scene.selection_index_changed.connect(self.update_selected_index)

        QtWidgets.QShortcut(QtCore.Qt.Key_Escape, self, activated=partial(self.m_scene.setCurrentInstruction, Instructions.No_Instruction))
        self.graphicsview = AnnotationView(self.m_scene)
        lay = QtWidgets.QVBoxLayout(self.disp_main)
        lay.addWidget(self.graphicsview)

    def Initialize(self):
         pas = publicArea.find()
         for pa in pas:
            self.cmb_areaName.addItem(pa.get('ParkingSpot'))

    def startWebCam(self):
        try:
            self.btn_stop.setEnabled(True)
            self.btn_start.setEnabled(False)
            if self.rab_cam.isChecked():
                global camIndex
                camIndex = self.camlist.currentIndex()
                self.Worker_Opencv = webcamThreadClass()
                self.Worker_Opencv.ImageUpdate.connect(self.opencv_emit)
                self.Worker_Opencv.FPS.connect(self.get_FPS)
                self.Worker_Opencv.start()
                self.rab_cam.setEnabled(False)
                self.rab_video.setEnabled(False)
            else:
                global videoPath
                self.Worker_Opencv = videoThreadClass()
                self.Worker_Opencv.ImageUpdate.connect(self.opencv_emit)
                self.Worker_Opencv.start()
                self.rab_cam.setEnabled(False)
                self.rab_video.setEnabled(False)
        except Exception as error :
            print(error)
    
    def stopWebcam(self):
        self.btn_start.setEnabled(True)
        self.btn_stop.setEnabled(False)
        self.rab_cam.setEnabled(True)
        self.rab_video.setEnabled(True)
        self.Worker_Opencv.stop()

    def importVideo(self):
        global videoPath
        path_temp = QtWidgets.QFileDialog.getOpenFileName(self, 'Open File')
        if path_temp[0] == '':
            return
        videoPath = path_temp[0]
        self.label_videoPath.setText(videoPath)

    def drawMode(self):
        global IsDrawingMode
        if IsDrawingMode == False:
            self.m_scene.setCurrentInstruction(Instructions.Polygon_Instruction)
            IsDrawingMode = True

    def detectionTest(self):
        if self.btn_stop.isEnabled() == True:
            self.stopWebcam()
        if hasattr(self, 'Worker_Test') and self.Worker_Test.isRunning():
            self.Worker_Test.stop()

        if self.rab_cam.isChecked():
            global camIndex
            camIndex = self.camlist.currentIndex()
            if self.label_configPath.text() == '':
                self.showMessageDialog("Please load or save the config file","Error")
                return
            fileName = self.getConfigFileName()
            file = configGridFs.find_one({"filename": fileName})
            if not file:
                self.showMessageDialog("Config file not found","Error")
                return
            data = pickle.loads(file.read())
            self.Worker_Test = camTestThreadClass()
            self.Worker_Test.data = data
            self.Worker_Test.start()
        else:
            self.rab_video.isChecked()
            global videoPath
            if videoPath == '':
                return
            if self.txt_testConfigPath.text() == '':
                return
            self.Worker_Test = videoTestThreadClass()
            self.Worker_Test.txt_testConfigPath = self.txt_testConfigPath
            self.Worker_Test.start()
            
            
    def importTestConfig(self):
        path_temp = QtWidgets.QFileDialog.getOpenFileName(self, 'Open File')
        if path_temp[0] == '':
            return
        self.txt_testConfigPath.setText(path_temp[0])

    
    def getCPU_usage(self,cpu):
        self.Qlabel_cpu.setText(str(cpu) + " %")
        if cpu > 15: self.Qlabel_cpu.setStyleSheet("color: rgb(23, 63, 95);")
        if cpu > 25: self.Qlabel_cpu.setStyleSheet("color: rgb(32, 99, 155);")
        if cpu > 45: self.Qlabel_cpu.setStyleSheet("color: rgb(60, 174, 163);")
        if cpu > 65: self.Qlabel_cpu.setStyleSheet("color: rgb(246, 213, 92);")
        if cpu > 85: self.Qlabel_cpu.setStyleSheet("color: rgb(237, 85, 59);")

    def getRAM_usage(self,ram):
        self.Qlabel_ram.setText(str(ram[2]) + " %")
        if ram[2] > 15: self.Qlabel_ram.setStyleSheet("color: rgb(23, 63, 95);")
        if ram[2] > 25: self.Qlabel_ram.setStyleSheet("color: rgb(32, 99, 155);")
        if ram[2] > 45: self.Qlabel_ram.setStyleSheet("color: rgb(60, 174, 163);")
        if ram[2] > 65: self.Qlabel_ram.setStyleSheet("color: rgb(246, 213, 92);")
        if ram[2] > 85: self.Qlabel_ram.setStyleSheet("color: rgb(237, 85, 59);")

    def get_FPS(self,fps):
        self.Qlabel_fps.setText(str(fps))
        if fps > 5: self.Qlabel_fps.setStyleSheet("color: rgb(237, 85, 59);")
        if fps > 15: self.Qlabel_fps.setStyleSheet("color: rgb(60, 174, 155);")
        if fps > 25: self.Qlabel_fps.setStyleSheet("color: rgb(85, 170, 255);")
        if fps > 35: self.Qlabel_fps.setStyleSheet("color: rgb(23, 63, 95);")

    def opencv_emit(self, Image):
        try:
            #QPixmap format           
            original = self.cvt_cv_qt(Image)
            self.graphicsview.setPixmap(original)
            #self.graphicsview.setScaledContents(True)
        except Exception as error:
            print(error)
    
    def clock(self):
        self.DateTime = QDateTime.currentDateTime()
        self.lcd_clock.display(self.DateTime.toString('hh:mm:ss'))

    def cvt_cv_qt(self, Image):
        rgb_img = cv2.cvtColor(src=Image,code=cv2.COLOR_BGR2RGB)
        h,w,ch = rgb_img.shape
        bytes_per_line = ch * w
        cvt2QtFormat = QImage(rgb_img.data, w, h, bytes_per_line, QImage.Format_RGB888)
        pixmap = QPixmap.fromImage(cvt2QtFormat)
        return pixmap #QPixmap.fromImage(cvt2QtFormat)
    
    def getConfigFileName(self):
        cameraName = self.txt_cameraName.text()
        areaType = self.cmb_areaType.currentText()
        areaName = self.cmb_areaName.currentText()
        if(cameraName == '' or areaType == '' or areaName == ''):
            self.showMessageDialog("Please fill in all the fields[CameraName, AreaType and AreaName]","Error")
            return
        fileName = cameraName + "_" + areaType + "_" + areaName
        if areaType == 'Private':
            if self.cmb_floor.currentIndex() == -1:
                self.showMessageDialog("Please select a floor","Error")
                return
            floor = self.cmb_floor.currentText()
            fileName = fileName + "_" + floor
        return fileName

    def update_selected_index(self, index):
        self.label_seletedIndex.setText(str(index))
        if self.label_configPath.text() == '':
            return
        if self.cmb_areaType.currentText() == 'Private':
            fileName = self.getConfigFileName()
            config = CamConfig.find_one({'name':fileName})
            if config:
                lotIds = config.get("lotIds")
                selectedIndex = self.label_seletedIndex.text()
                if selectedIndex in lotIds:
                    self.label_lotId.setText(lotIds[selectedIndex])
                else:
                    self.label_lotId.setText('')
        


    def saveConfig(self):
        polylines = []
        if polyitems == []:
            self.showMessageDialog("Please draw polygons before saving","Error")
            return
        
        for item in polyitems:
            points = []
            for i in range(item.number_of_points()):
                points.append([item.m_items[i].x(),item.m_items[i].y()])
            polylines.append(np.array(points,np.int32))
            area_names.append(item.index)
        data={'polylines':polylines,'area_names':area_names}
        if self.rab_cam.isChecked():
            areaType = self.cmb_areaType.currentText()
            floor = self.cmb_floor.currentText()
            fileName = self.getConfigFileName()
            if fileName == None:
                return
            file = configGridFs.find_one({"filename": fileName})
            if file:
                configGridFs.delete(file._id)
            configGridFs.put(pickle.dumps(data),filename=fileName)
            self.label_configPath.setText(fileName)
            if areaType == 'Private':
                self.label_floor.setText(floor)
                self.showMessageDialog("Config file saved successfully","Success")
            else:
                cameraName = self.txt_cameraName.text()
                areaName = self.cmb_areaName.currentText()
                camDeviceName = self.online_cam[self.camlist.currentIndex()].deviceName()
                config = CamConfig.find_one({'name':fileName})
                area = publicArea.find_one({'ParkingSpot':areaName})
                myDict = {"name": fileName,
                          "displayName": cameraName,
                          "areaType": areaType,
                          "areaId": area.get('_id'),
                          "camDeviceName": camDeviceName}
                if config:
                    CamConfig.find_one_and_update({'name':fileName},{'$set':myDict})
                    self.showMessageDialog("Camera(" + cameraName +") updated successfully","Success")
                else:
                    CamConfig.insert_one(myDict)
                    self.showMessageDialog("Camera(" + cameraName +") is set to area(" + areaName + ")successfully","Success")
        else:
            name = QtWidgets.QFileDialog.getSaveFileName(self, 'Save File')
            if name[0] == '':
                return
            with open(name[0],"wb") as f:
                pickle.dump(data,f)
            self.label_configPath.setText(name[0])
            self.showMessageDialog("Config file saved successfully","Success")
            
    def loadConfig(self):
        data = None
        if self.rab_cam.isChecked():
            areaType = self.cmb_areaType.currentText()
            fileName = self.getConfigFileName()
            file = configGridFs.find_one({"filename": fileName})
            if not file:
                self.showMessageDialog("Config file not found","Error")
                return
            data = pickle.loads(file.read())
            self.label_configPath.setText(fileName)
            if areaType == 'Private':
                floor = self.cmb_floor.currentText()
                self.label_floor.setText(floor)
        else:
            name = QtWidgets.QFileDialog.getOpenFileName(self, 'Open File')
            if name[0] == '':
                return
            with open(name[0],"rb") as f:
                for item in self.m_scene.items():
                    if isinstance(item, PolygonAnnotation) or isinstance(item, GripItem):
                        self.m_scene.removeItem(item)
                data = pickle.load(f)
            self.label_configPath.setText(name[0])
        self.m_scene.clearPolygons()
        polylines = data['polylines']
        area_names = data['area_names']
        for i in range(len(polylines)):
            self.m_scene.setCurrentInstruction(Instructions.Polygon_Instruction)
            self.m_scene.polygon_item.setText(area_names[i])
            for point in polylines[i]:
                self.m_scene.polygon_item.removeLastPoint()
                self.m_scene.polygon_item.addPoint(QtCore.QPointF(point[0],point[1]))
                # movable element
                self.m_scene.polygon_item.addPoint(QtCore.QPointF(point[0],point[1]))
            self.m_scene.setCurrentInstruction(Instructions.No_Instruction)

    def areaTypeChanged(self):
        if self.cmb_areaType.currentIndex() == 0:
            self.bnt_loadArea.setEnabled(False)
            self.cmb_lotId.setEnabled(False)
            self.cmb_floor.setEnabled(False)
            self.cmb_lotId.clear()
            self.cmb_floor.clear()
            self.loadArea()
        else:
            self.bnt_loadArea.setEnabled(True)
            self.cmb_lotId.setEnabled(True)
            self.cmb_floor.setEnabled(True)
            self.loadArea()
        
    
    def loadArea(self):
        try:
            if self.cmb_areaName.count() > 0:
                self.cmb_areaName.clear()   
            if self.cmb_areaType.currentIndex() == 0:
                pas = publicArea.find()
                
                for pa in pas:
                    self.cmb_areaName.addItem(pa.get('ParkingSpot'))
            else:
                pas = privateArea.find()
                for pa in pas:
                    self.cmb_areaName.addItem(pa.get('CompanyName'))
        except Exception as error:
            print(error)

    def loadPrivateArea(self):
        try:
            if self.cmb_areaName.count() > 0:
                if self.cmb_areaName.currentText == '':
                    return
                area = privateArea.find_one({'CompanyName':self.cmb_areaName.currentText()})
                self.cmb_floor.clear()
                self.cmb_lotId.clear()

                fis = area.get("FloorInfo")
                for fi in fis:
                    self.cmb_floor.addItem(fi.get("Floor"))
        except Exception as error:
            print(error)

    def floorChanged(self):
        if self.cmb_floor.currentIndex() == -1:
            return
        area = privateArea.find_one({'CompanyName':self.cmb_areaName.currentText()})
        fis = area.get("FloorInfo")
        self.cmb_lotId.clear()
        for rec in fis[self.cmb_floor.currentIndex()].get("Rectangles"):
                self.cmb_lotId.addItem(str(rec.get("Index")))

    def bindCam(self):
        if self.label_configPath.text() == '':
            self.showMessageDialog("Please load or save the config file","Error")
            return
        if self.label_seletedIndex.text() == '':
            self.showMessageDialog("Please select a polygon","Error")
            return
        if self.txt_cameraName.text() == '':
            self.showMessageDialog("Please enter the camera name","Error")
            return

        cameraName = self.txt_cameraName.text()
        areaType = self.cmb_areaType.currentText()
        areaName = self.cmb_areaName.currentText()
        fileName = cameraName + "_" + areaType + "_" + areaName
        camDeviceName = self.online_cam[self.camlist.currentIndex()].deviceName()
        if areaType == 'Private':
            floor = self.cmb_floor.currentText()
            fileName = fileName + "_" + floor
        config = CamConfig.find_one({'name':fileName})
       
        
        area = privateArea.find_one({'CompanyName':areaName})
        lotId = self.cmb_lotId.currentText()
        selectedIndex = self.label_seletedIndex.text()

        if config:
            lotIds = config.get("lotIds")
            lotIds[selectedIndex] = lotId
            mydict = {"name": fileName,
                  "displayName": cameraName,
                  "areaType": areaType,
                  "areaId": area.get('_id'),
                  "camDeviceName": camDeviceName,
                  "lotIds": lotIds}
            CamConfig.find_one_and_update({'name':fileName},{'$set':mydict})
            self.showMessageDialog("Camera(" + cameraName +") updated successfully","Success")
        else:
            lotIds = {selectedIndex: lotId}
            mydict = {"name": fileName, 
             "displayName": cameraName,
             "areaType": areaType,
             "areaId": area.get('_id'),
             "camDeviceName": camDeviceName,
             "lotIds": lotIds}
            CamConfig.insert_one(mydict)
            self.showMessageDialog("Camera(" + cameraName +") is set to area(" + areaName + ")successfully","Success")
        self.label_lotId.setText(lotId)
    
    def DbTest(self):
        if hasattr(self, 'Worker_Test') and self.Worker_Test.isRunning():
            self.Worker_Test.stop()
            return
        
        if self.rab_cam.isChecked():
            global camIndex
            camIndex = self.camlist.currentIndex()
            self.Worker_Opencv = camTestThreadClass()
            self.Worker_Opencv.start()
        else:
            global videoPath
            if videoPath == '':
                return
            if self.txt_testConfigPath.text() == '':
                return
            self.Worker_Test = videoDbTestThreadClass()
            self.Worker_Test.txt_testConfigPath = self.txt_testConfigPath
            self.Worker_Test.start()

    def showMessageDialog(self, msg, title):
        msgBox = QMessageBox()
        msgBox.setIcon(QMessageBox.Information)
        msgBox.setText(msg)
        msgBox.setWindowTitle(title)
        msgBox.setStandardButtons(QMessageBox.Ok)
        msgBox.exec()

class AnnotationScene(QtWidgets.QGraphicsScene):
    def __init__(self, parent=None):
        super(AnnotationScene, self).__init__(parent)
        self.current_instruction = Instructions.No_Instruction
        self.selectionChanged.connect(self.selection_changed)
       
    selection_index_changed = pyqtSignal(str)

    def setCurrentInstruction(self, instruction):
        if instruction == Instructions.No_Instruction:
            global IsDrawingMode,polygon_index
            self.polygon_item.removeLastPoint()
            IsDrawingMode = False
            
        self.current_instruction = instruction
        if instruction == Instructions.Polygon_Instruction:
            self.polygon_item = PolygonAnnotation()
            self.addItem(self.polygon_item)
            polyitems.append(self.polygon_item)
            self.polygon_item.setText(str(polygon_index))
            polygon_index += 1

    def mousePressEvent(self, event):
        if self.current_instruction == Instructions.Polygon_Instruction:
            self.polygon_item.removeLastPoint()
            self.polygon_item.addPoint(event.scenePos())
            # movable element
            self.polygon_item.addPoint(event.scenePos())
        super(AnnotationScene, self).mousePressEvent(event)

    def mouseMoveEvent(self, event):
        if self.current_instruction == Instructions.Polygon_Instruction:
            self.polygon_item.movePoint(self.polygon_item.number_of_points()-1, event.scenePos())
        super(AnnotationScene, self).mouseMoveEvent(event)
    
    def selection_changed(self):
        for item in self.selectedItems():
            if isinstance(item, PolygonAnnotation):
                self.selection_index_changed.emit(item.index)  # Emit signal with selected index
                break
        
    def clearPolygons(self):
        global polygon_index
        
        for item in self.items():
            if isinstance(item, PolygonAnnotation) or isinstance(item, GripItem):
                self.removeItem(item)
        polyitems.clear()
        polygon_index = 1

class boardInfoClass(QThread):
    cpu = pyqtSignal(float)
    ram = pyqtSignal(tuple)
    temp = pyqtSignal(float)
    
    def run(self):
        self.ThreadActive = True
        while self.ThreadActive:
            cpu = sysinfo.getCPU()
            ram = sysinfo.getRAM()
            #temp = sysinfo.getTemp()
            self.cpu.emit(cpu)
            self.ram.emit(ram)
            #self.temp.emit(temp)

    def stop(self):
        self.ThreadActive = False
        self.quit()

class camTestThreadClass(QThread):
    def run(self):
        self.ThreadActive = True
        parkingLot_detect_cam(camIndex,self.data)
    def stop(self):
        self.ThreadActive = False
        self.quit()

class videoTestThreadClass(QThread):
    def run(self):
        self.ThreadActive = True
        parkingLot_detect_video(videoPath,self.txt_testConfigPath.text())
    def stop(self):
        self.ThreadActive = False
        self.quit()

class videoDbTestThreadClass(QThread):
    def run(self):
        self.ThreadActive = True
        cmd = f'start cmd /k python -c "import yolo; yolo.start_detect_video(\'{videoPath}\', \'{self.txt_testConfigPath.text()}\', \'Private\', \'666763d4d2c61b754e32a094\')"'
        subprocess.Popen(cmd, shell=True)
    def stop(self):
        self.ThreadActive = False
        stopTesting()
        self.quit()

class webcamThreadClass(QThread):
    ImageUpdate = pyqtSignal(np.ndarray)
    FPS = pyqtSignal(int)
    global camIndex
   
    def run(self):
        Capture = cv2.VideoCapture(camIndex,cv2.CAP_DSHOW)
        Capture.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
        Capture.set(cv2.CAP_PROP_FRAME_HEIGHT, 960)
        self.ThreadActive = True
        prev_frame_time = 0
        new_frame_time = 0
        while self.ThreadActive:
            ret,frame = Capture.read()
            if ret:
                new_frame_time = time.time()
                fps = 1/(new_frame_time-prev_frame_time)
                prev_frame_time = new_frame_time
                self.ImageUpdate.emit(frame)
                self.FPS.emit(fps)
    
    def stop(self):
        self.ThreadActive = False
        self.quit()

class videoThreadClass(QThread):
    ImageUpdate = pyqtSignal(np.ndarray)
    global videoPath
    def run(self):
        Capture = cv2.VideoCapture(videoPath)
        Capture.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
        Capture.set(cv2.CAP_PROP_FRAME_HEIGHT, 960)
        self.ThreadActive = True
        prev_frame_time = 0
        new_frame_time = 0
        while self.ThreadActive: 
            ret,frame = Capture.read()
            if ret:
                self.ImageUpdate.emit(frame)
    
    def stop(self):
        self.ThreadActive = False
        self.quit() 

if __name__ == '__main__':
    app = QApplication([])
    window = MainWindow()
    window.show()
    app.exec_()