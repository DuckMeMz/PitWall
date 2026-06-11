using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace PitWall.Commands;

public class RelayCommand : ICommand
{
    private readonly Action callback;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action _callback)
    {
        if (_callback == null) return;
        callback = _callback;
    }

    public bool CanExecute(object? _parameter)
    {
        return callback != null;
    }

    public void Execute(object? _parameter)
    {
        callback();
    }
}