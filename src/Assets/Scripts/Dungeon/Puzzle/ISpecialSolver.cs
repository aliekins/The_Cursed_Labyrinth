using System;

public interface ISpecialSolver
{
    /// Raised when the puzzle is completed.
    event Action OnSolved;
}