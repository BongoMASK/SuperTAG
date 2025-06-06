using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugCommandbase {
    private string _commandId;
    private string _commandDescription;
    private string _commandFormat;

    public string commandId { get { return _commandId; } }
    public string commandDescription { get { return _commandDescription; } }
    public string commandFormat { get { return _commandFormat; } }

    public DebugCommandbase(string id, string description, string format) {
        _commandId = id;
        _commandDescription = description;
        _commandFormat = format;
    }
}

public class DebugCommand : DebugCommandbase {
    private Action command;

    public DebugCommand(string id, string description, string format, Action command) : base(id, description, format) {
        this.command = command;
    }

    public void Invoke() {
        command.Invoke();
    }
}

public class DebugCommand<T1> : DebugCommandbase {
    private Action<T1> command;

    public DebugCommand(string id, string description, string format, Action<T1> command) : base(id, description, format) {
        this.command = command;
    }

    public void Invoke(T1 value) {
        command.Invoke(value);
    }
}

public class DebugCommand<T1, T2> : DebugCommandbase {
    private Action<T1, T2> command;

    public DebugCommand(string id, string description, string format, Action<T1, T2> command) : base(id, description, format) {
        this.command = command;
    }

    public void Invoke(T1 value, T2 value2) {
        command.Invoke(value, value2);
    }
}
