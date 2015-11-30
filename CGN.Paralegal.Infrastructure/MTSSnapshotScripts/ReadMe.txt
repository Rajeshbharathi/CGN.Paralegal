
1.The script is invoked as follows
C:\LexisNexisProjects\cnev\powershell\QuiesceVivisimo.ps1 -mode N -config "C:\LexisNexisProjects\cnev\powershell\vivisimo_instances_config.xml"

  Where,
–mode can be Y or N. Y means make all the collections read only. N means bring them back to normal read write mode.
-config is an optional parameter for the full path to the config file. If –config is not specified then the script will try to read the config file from the same location as the script file itself.

2.The script uses a config file (in xml) that has configurations like error file location, various Vivisimo instances, port, admin and password.

3.The script invokes the rest api command for disabling/enabling all the collections for each instance of the Vivisimo specified in the config file.

4.If any exception is thrown by the Vivisimo server then that is logged in the error file.

