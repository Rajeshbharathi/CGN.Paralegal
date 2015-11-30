
@echo off

XmlPreprocess.exe /c /i ..\web.config    /x EVCPathList.xml /e %1 /o ..\web.config
XmlPreprocess.exe /c /i ..\common.config /x EVCPathList.xml /e %1 /o ..\common.config
