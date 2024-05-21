#QtDesinger Reference: https://github.com/Phatthawat/OBJECT-COUNTING-MACHINE-USING-COMPUTER-VISION
#Parking lot Reference: https://www.youtube.com/watch?v=MyvylXVWYjY&t=3000s
#Polygon Reference: https://stackoverflow.com/questions/52751121/pyqt-user-editable-polygons

import cv2,time,sys,sysinfo,cvzone
from PyQt5 import uic,QtGui,QtCore,QtWidgets
from PyQt5.QtMultimedia import QCameraInfo
from PyQt5.QtWidgets import QApplication, QMainWindow, QDialog
from PyQt5.QtCore import QThread, pyqtSignal, pyqtSlot, QTimer, QDateTime, Qt
from PyQt5.QtGui import QImage, QPixmap, QPainter, QPen
import numpy as np
from enum import Enum
from functools import partial
import pickle
from yolo import parkingLot_detect_video


polyitems = []

area_names=[]
points = []
drawing = False
polygon_index = 1

class GripItem(QtWidgets.QGraphicsPathItem):
    circle = QtGui.QPainterPath()
    circle.addEllipse(QtCore.QRectF(-5, -5, 10, 10))
    square = QtGui.QPainterPath()
    square.addRect(QtCore.QRectF(-5, -5, 10, 10))

    def __init__(self, annotation_item, index):
        super(GripItem, self).__init__()
        self.m_annotation_item = annotation_item
        self.m_index = index

        self.setPath(GripItem.circle)
        self.setBrush(QtGui.QColor("green"))
        self.setPen(QtGui.QPen(QtGui.QColor("green"), 2))
        self.setFlag(QtWidgets.QGraphicsItem.ItemIsSelectable, True)
        self.setFlag(QtWidgets.QGraphicsItem.ItemIsMovable, True)
        self.setFlag(QtWidgets.QGraphicsItem.ItemSendsGeometryChanges, True)
        self.setAcceptHoverEvents(True)
        self.setZValue(11)
        self.setCursor(QtGui.QCursor(QtCore.Qt.PointingHandCursor))

    def hoverEnterEvent(self, event):
        self.setPath(GripItem.square)
        self.setBrush(QtGui.QColor("red"))
        super(GripItem, self).hoverEnterEvent(event)

    def hoverLeaveEvent(self, event):
        self.setPath(GripItem.circle)
        self.setBrush(QtGui.QColor("green"))
        super(GripItem, self).hoverLeaveEvent(event)

    def mouseReleaseEvent(self, event):
        self.setSelected(False)
        super(GripItem, self).mouseReleaseEvent(event)

    def itemChange(self, change, value):
        if change == QtWidgets.QGraphicsItem.ItemPositionChange and self.isEnabled():
            self.m_annotation_item.movePoint(self.m_index, value)
        return super(GripItem, self).itemChange(change, value)

class PolygonAnnotation(QtWidgets.QGraphicsPolygonItem):
    def __init__(self, parent=None):
        super(PolygonAnnotation, self).__init__(parent)
        self.m_points = []
        self.setZValue(10)
        self.setPen(QtGui.QPen(QtGui.QColor("green"), 2))
        self.setAcceptHoverEvents(True)

        self.setFlag(QtWidgets.QGraphicsItem.ItemIsSelectable, True)
        self.setFlag(QtWidgets.QGraphicsItem.ItemIsMovable, True)
        self.setFlag(QtWidgets.QGraphicsItem.ItemSendsGeometryChanges, True)

        self.setCursor(QtGui.QCursor(QtCore.Qt.PointingHandCursor))
        self.m_items = []
        self._index = 0
         # Create a QGraphicsTextItem
        self.text_item = QtWidgets.QGraphicsTextItem(self)
        self.text_item.setDefaultTextColor(QtGui.QColor("green"))

    @property
    def index(self):
        return self._index

    def number_of_points(self):
        return len(self.m_items)

    def addPoint(self, p):
        self.m_points.append(p)
        self.setPolygon(QtGui.QPolygonF(self.m_points))
        item = GripItem(self, len(self.m_points) - 1)
        self.scene().addItem(item)
        self.m_items.append(item)
        item.setPos(p)

        # Update the position of the text item
        self.updateTextPosition()

    def removeLastPoint(self):
        if self.m_points:
            self.m_points.pop()
            self.setPolygon(QtGui.QPolygonF(self.m_points))
            it = self.m_items.pop()
            self.scene().removeItem(it)
            del it

    def movePoint(self, i, p):
        if 0 <= i < len(self.m_points):
            self.m_points[i] = self.mapFromScene(p)
            self.setPolygon(QtGui.QPolygonF(self.m_points))

        # Update the position of the text item
        self.updateTextPosition()

    def move_item(self, index, pos):
        if 0 <= index < len(self.m_items):
            item = self.m_items[index]
            item.setEnabled(False)
            item.setPos(pos)
            item.setEnabled(True)
        self.updateTextPosition()

    def itemChange(self, change, value):
        if change == QtWidgets.QGraphicsItem.ItemPositionHasChanged:
            for i, point in enumerate(self.m_points):
                self.move_item(i, self.mapToScene(point))
        return super(PolygonAnnotation, self).itemChange(change, value)

    def hoverEnterEvent(self, event):
        self.setBrush(QtGui.QColor(255, 0, 0, 100))
        super(PolygonAnnotation, self).hoverEnterEvent(event)

    def hoverLeaveEvent(self, event):
        self.setBrush(QtGui.QBrush(QtCore.Qt.NoBrush))
        super(PolygonAnnotation, self).hoverLeaveEvent(event)
    
    def updateTextPosition(self):
        # Adjust the position of the text item
        self.text_item.setPos(self.boundingRect().center() - self.text_item.boundingRect().center())
    
    def setText(self, text):
        self._index = text
        self.text_item.setPlainText(text)

class Instructions(Enum):
    No_Instruction = 0
    Polygon_Instruction = 1

class AnnotationScene(QtWidgets.QGraphicsScene):
    def __init__(self, parent=None):
        super(AnnotationScene, self).__init__(parent)
        # self.image_item = QtWidgets.QGraphicsPixmapItem()
        # self.image_item.setCursor(QtGui.QCursor(QtCore.Qt.CrossCursor))
        # self.addItem(self.image_item)
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

class AnnotationView(QtWidgets.QGraphicsView):
    factor = 2.0

    def __init__(self, scene, parent=None):
        super(AnnotationView, self).__init__(parent)
        self.setRenderHints(QtGui.QPainter.Antialiasing | QtGui.QPainter.SmoothPixmapTransform)
        self.setMouseTracking(True)
        QtWidgets.QShortcut(QtGui.QKeySequence.ZoomIn, self, activated=self.zoomIn)
        QtWidgets.QShortcut(QtGui.QKeySequence.ZoomOut, self, activated=self.zoomOut)
        self.setScene(scene)

        self._pixmap_item = QtWidgets.QGraphicsPixmapItem()
        scene.addItem(self.pixmap_item)

    @property
    def pixmap_item(self):
        return self._pixmap_item

    @QtCore.pyqtSlot()
    def zoomIn(self):
        self.zoom(AnnotationView.factor)

    @QtCore.pyqtSlot()
    def zoomOut(self):
        self.zoom(1 / AnnotationView.factor)

    def zoom(self, f):
        self.scale(f, f)
        if self.scene() is not None:
            self.centerOn(self.scene().image_item)
            
    def setPixmap(self, pixmap):
        self.pixmap_item.setPixmap(pixmap)

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
            

class MainWindow(QMainWindow):
    def __init__(self, *args, obj=None, **kwargs):
        super(MainWindow, self).__init__(*args, **kwargs)
        global IsDrawingMode
        IsDrawingMode = False
        #super().__init__()
        # Load the UI
        self.ui = uic.loadUi("MainWindow.ui",self)
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

    def startWebCam(self,pin):
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
    
    def stopWebcam(self,pin):
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

    def drawMode(self):
        global IsDrawingMode
        if IsDrawingMode == False:
            self.m_scene.setCurrentInstruction(Instructions.Polygon_Instruction)
            IsDrawingMode = True

    def detectionTest(self):
        if self.rab_cam.isChecked():
            print("Camera")
        else:
            self.rab_video.isChecked()
            global videoPath
            if videoPath == '':
                return
            if self.txt_testConfigPath.text() == '':
                return
            parkingLot_detect_video(videoPath,self.txt_testConfigPath.text())
            
            
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

    def update_selected_index(self, index):
        self.label_seletedIndex.setText(str(index))

    def saveConfig(self):
        name = QtWidgets.QFileDialog.getSaveFileName(self, 'Save File')
        if name[0] == '':
            return
        polylines = []
        for item in polyitems:
            points = []
            for i in range(item.number_of_points()):
                points.append([item.m_items[i].x(),item.m_items[i].y()])

            polylines.append(np.array(points,np.int32))
            area_names.append(item.index)
        with open(name[0],"wb") as f:
            data={'polylines':polylines,'area_names':area_names}
            pickle.dump(data,f)
    
    def loadConfig(self):
        name = QtWidgets.QFileDialog.getOpenFileName(self, 'Open File')
        if name[0] == '':
            return
        with open(name[0],"rb") as f:
            for item in self.m_scene.items():
                if isinstance(item, PolygonAnnotation) or isinstance(item, GripItem):
                    self.m_scene.removeItem(item)
            data = pickle.load(f)
            polylines = data['polylines']
            area_names = data['area_names']
            for i in range(len(polylines)):
                self.m_scene.setCurrentInstruction(Instructions.Polygon_Instruction)
                # polyitems[-1].addPoint = polylines[i]
                # polyitems[-1].setPolygon(QtGui.QPolygonF(polyitems[-1].m_points))
                self.m_scene.polygon_item.setText(area_names[i])
                for point in polylines[i]:
                    self.m_scene.polygon_item.removeLastPoint()
                    self.m_scene.polygon_item.addPoint(QtCore.QPointF(point[0],point[1]))
                    # movable element
                    self.m_scene.polygon_item.addPoint(QtCore.QPointF(point[0],point[1]))

                self.m_scene.setCurrentInstruction(Instructions.No_Instruction)

if __name__ == '__main__':
    app = QApplication([])
    window = MainWindow()
    window.show()
    app.exec_()