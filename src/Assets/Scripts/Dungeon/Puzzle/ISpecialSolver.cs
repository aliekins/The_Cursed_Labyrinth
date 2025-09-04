using System;
/**
 * @file ISpecialSolver.cs
 * @brief contract for special room solvers to signal completion
 * @ingroup Puzzle
 */
public interface ISpecialSolver
{
    event Action OnSolved;
}