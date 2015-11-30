dim objshell

Set objshell = CreateObject("wscript.shell")
objshell.run ".\ConfigureWCF.cmd"

set objshell=nothing
