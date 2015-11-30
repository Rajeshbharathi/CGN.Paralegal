
@echo off


XmlPreprocess.exe /c /x EVCPathList.xml /e %1 /i ..\Director\Director.exe.config
XmlPreprocess.exe /c /x EVCPathList.xml /e %1 /i ..\Director\common.config

XmlPreprocess.exe /c /x EVCPathList.xml /e %1 /i ..\Workers\WorkerManager.exe.config
XmlPreprocess.exe /c /x EVCPathList.xml /e %1 /i ..\Workers\common.config