using System;
using SeqCli.Cli;

namespace SeqCli.Tests.Support;

class ActionCommand : Command
{
    public ActionCommand(Action action)
    {
        action.Invoke();
    }
}