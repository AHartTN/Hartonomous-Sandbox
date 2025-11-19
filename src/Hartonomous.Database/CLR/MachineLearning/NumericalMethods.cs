using System;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Numerical methods for integration, optimization, and root finding.
    /// 
    /// UNIVERSAL DISTANCE SUPPORT: All convergence tests use configurable distance metrics,
    /// enabling these methods to work in semantic spaces, embedding spaces, and parameter spaces
    /// across all modalities (text, image, audio, code, weights).
    /// </summary>
    internal static class NumericalMethods
    {
        #region Integration

        /// <summary>
        /// Euler integration for state evolution in N-dimensional space.
        /// 
        /// APPLICATIONS IN USER'S ARCHITECTURE:
        /// - State evolution: Continuous generation path through embedding space
        /// - Trajectory prediction: Next-token prediction as continuous flow
        /// - Semantic drift: Track concept evolution over time
        /// - Parameter updates: Simple gradient descent step
        /// </summary>
        /// <param name="initialState">Starting state vector</param>
        /// <param name="derivative">Function computing derivative (velocity) at each state</param>
        /// <param name="timeStep">Integration time step (dt)</param>
        /// <param name="numSteps">Number of integration steps</param>
        /// <param name="metric">Distance metric for convergence check (null = Euclidean)</param>
        /// <param name="convergenceThreshold">Stop if state change < threshold (0 = no early stop)</param>
        /// <returns>Final state after integration</returns>
        public static float[] EulerIntegration(
            float[] initialState,
            Func<float[], float[]> derivative,
            double timeStep,
            int numSteps,
            IDistanceMetric? metric = null,
            double convergenceThreshold = 0.0)
        {
            metric = metric ?? new EuclideanDistance();

            var state = new float[initialState.Length];
            Array.Copy(initialState, state, initialState.Length);

            for (int step = 0; step < numSteps; step++)
            {
                var deriv = derivative(state);
                var nextState = new float[state.Length];

                // Euler step: x_{n+1} = x_n + dt * f(x_n)
                for (int i = 0; i < state.Length; i++)
                    nextState[i] = state[i] + (float)(timeStep * deriv[i]);

                // Convergence check
                if (convergenceThreshold > 0)
                {
                    double change = metric.Distance(state, nextState);
                    if (change < convergenceThreshold)
                        return nextState;
                }

                state = nextState;
            }

            return state;
        }

        /// <summary>
        /// Runge-Kutta 2nd order (RK2) integration - midpoint method.
        /// More accurate than Euler for smooth dynamics.
        /// </summary>
        /// <param name="initialState">Starting state vector</param>
        /// <param name="derivative">Function computing derivative at each state</param>
        /// <param name="timeStep">Integration time step</param>
        /// <param name="numSteps">Number of integration steps</param>
        /// <param name="metric">Distance metric for convergence check</param>
        /// <param name="convergenceThreshold">Stop if state change < threshold</param>
        /// <returns>Final state after integration</returns>
        public static float[] AdamsBashforth2(
            float[] initialState,
            Func<float[], float[]> derivative,
            double timeStep,
            int numSteps,
            IDistanceMetric? metric = null,
            double convergenceThreshold = 0.0)
        {
            metric = metric ?? new EuclideanDistance();

            var state = new float[initialState.Length];
            Array.Copy(initialState, state, initialState.Length);

            for (int step = 0; step < numSteps; step++)
            {
                // k1 = f(x_n)
                var k1 = derivative(state);

                // Midpoint: x_mid = x_n + dt/2 * k1
                var midState = new float[state.Length];
                for (int i = 0; i < state.Length; i++)
                    midState[i] = state[i] + (float)(timeStep / 2.0 * k1[i]);

                // k2 = f(x_mid)
                var k2 = derivative(midState);

                // x_{n+1} = x_n + dt * k2
                var nextState = new float[state.Length];
                for (int i = 0; i < state.Length; i++)
                    nextState[i] = state[i] + (float)(timeStep * k2[i]);

                // Convergence check
                if (convergenceThreshold > 0)
                {
                    double change = metric.Distance(state, nextState);
                    if (change < convergenceThreshold)
                        return nextState;
                }

                state = nextState;
            }

            return state;
        }

        /// <summary>
        /// Runge-Kutta 4th order (RK4) integration - classic method.
        /// Highest accuracy for smooth dynamics, standard for ODE solving.
        /// </summary>
        /// <param name="initialState">Starting state vector</param>
        /// <param name="derivative">Function computing derivative at each state</param>
        /// <param name="timeStep">Integration time step</param>
        /// <param name="numSteps">Number of integration steps</param>
        /// <param name="metric">Distance metric for convergence check</param>
        /// <param name="convergenceThreshold">Stop if state change < threshold</param>
        /// <returns>Final state after integration</returns>
        public static float[] RungeKutta4(
            float[] initialState,
            Func<float[], float[]> derivative,
            double timeStep,
            int numSteps,
            IDistanceMetric? metric = null,
            double convergenceThreshold = 0.0)
        {
            metric = metric ?? new EuclideanDistance();

            var state = new float[initialState.Length];
            Array.Copy(initialState, state, initialState.Length);

            for (int step = 0; step < numSteps; step++)
            {
                // k1 = f(x_n)
                var k1 = derivative(state);

                // k2 = f(x_n + dt/2 * k1)
                var temp = new float[state.Length];
                for (int i = 0; i < state.Length; i++)
                    temp[i] = state[i] + (float)(timeStep / 2.0 * k1[i]);
                var k2 = derivative(temp);

                // k3 = f(x_n + dt/2 * k2)
                for (int i = 0; i < state.Length; i++)
                    temp[i] = state[i] + (float)(timeStep / 2.0 * k2[i]);
                var k3 = derivative(temp);

                // k4 = f(x_n + dt * k3)
                for (int i = 0; i < state.Length; i++)
                    temp[i] = state[i] + (float)(timeStep * k3[i]);
                var k4 = derivative(temp);

                // x_{n+1} = x_n + dt/6 * (k1 + 2*k2 + 2*k3 + k4)
                var nextState = new float[state.Length];
                for (int i = 0; i < state.Length; i++)
                    nextState[i] = state[i] + (float)(timeStep / 6.0 * (k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i]));

                // Convergence check
                if (convergenceThreshold > 0)
                {
                    double change = metric.Distance(state, nextState);
                    if (change < convergenceThreshold)
                        return nextState;
                }

                state = nextState;
            }

            return state;
        }

        #endregion

        #region Root Finding

        /// <summary>
        /// Newton-Raphson root finding in N-dimensional space.
        /// 
        /// APPLICATIONS:
        /// - Finding equilibrium states in semantic space
        /// - Solving constraint satisfaction problems
        /// - Optimizing generation objectives
        /// </summary>
        /// <param name="initialGuess">Starting point for iteration</param>
        /// <param name="function">Function to find root of (f(x) = 0)</param>
        /// <param name="jacobian">Jacobian matrix function (derivative of f)</param>
        /// <param name="maxIterations">Maximum iterations</param>
        /// <param name="metric">Distance metric for convergence</param>
        /// <param name="tolerance">Convergence tolerance</param>
        /// <returns>Approximate root, or null if not converged</returns>
        public static float[]? NewtonRaphson(
            float[] initialGuess,
            Func<float[], float[]> function,
            Func<float[], float[][]> jacobian,
            int maxIterations = 100,
            IDistanceMetric? metric = null,
            double tolerance = 1e-6)
        {
            metric = metric ?? new EuclideanDistance();

            var x = new float[initialGuess.Length];
            Array.Copy(initialGuess, x, initialGuess.Length);

            for (int iter = 0; iter < maxIterations; iter++)
            {
                var fx = function(x);
                var J = jacobian(x);

                // Solve J * delta = -f(x) for delta (simple Gaussian elimination for small systems)
                var delta = SolveLinearSystem(J, fx);
                if (delta == null)
                    return null; // Singular Jacobian

                // x_{n+1} = x_n - delta
                var nextX = new float[x.Length];
                for (int i = 0; i < x.Length; i++)
                    nextX[i] = x[i] - delta[i];

                // Convergence check
                double change = metric.Distance(x, nextX);
                if (change < tolerance)
                    return nextX;

                x = nextX;
            }

            return null; // Did not converge
        }

        /// <summary>
        /// Bisection method for 1D root finding (scalar version).
        /// Robust but slower than Newton-Raphson.
        /// </summary>
        /// <param name="function">Scalar function to find root of</param>
        /// <param name="lowerBound">Lower search bound</param>
        /// <param name="upperBound">Upper search bound</param>
        /// <param name="tolerance">Convergence tolerance</param>
        /// <param name="maxIterations">Maximum iterations</param>
        /// <returns>Approximate root, or null if not found</returns>
        public static double? Bisection(
            Func<double, double> function,
            double lowerBound,
            double upperBound,
            double tolerance = 1e-6,
            int maxIterations = 100)
        {
            double fLower = function(lowerBound);
            double fUpper = function(upperBound);

            // Ensure opposite signs
            if (fLower * fUpper > 0)
                return null;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                double mid = (lowerBound + upperBound) / 2.0;
                double fMid = function(mid);

                if (Math.Abs(fMid) < tolerance)
                    return mid;

                if (fMid * fLower < 0)
                {
                    upperBound = mid;
                    fUpper = fMid;
                }
                else
                {
                    lowerBound = mid;
                    fLower = fMid;
                }

                if (Math.Abs(upperBound - lowerBound) < tolerance)
                    return (lowerBound + upperBound) / 2.0;
            }

            return (lowerBound + upperBound) / 2.0;
        }

        #endregion

        #region Optimization

        /// <summary>
        /// Gradient descent optimization in N-dimensional space.
        /// 
        /// APPLICATIONS IN USER'S ARCHITECTURE:
        /// - Parameter optimization for model fine-tuning
        /// - Embedding space navigation toward target
        /// - Loss minimization in semantic space
        /// - Adversarial example generation
        /// </summary>
        /// <param name="initialPoint">Starting point</param>
        /// <param name="gradient">Gradient function (negative points toward minimum)</param>
        /// <param name="learningRate">Step size</param>
        /// <param name="maxIterations">Maximum iterations</param>
        /// <param name="metric">Distance metric for convergence</param>
        /// <param name="tolerance">Convergence tolerance</param>
        /// <returns>Optimized point</returns>
        public static float[] GradientDescent(
            float[] initialPoint,
            Func<float[], float[]> gradient,
            double learningRate,
            int maxIterations = 1000,
            IDistanceMetric? metric = null,
            double tolerance = 1e-6)
        {
            metric = metric ?? new EuclideanDistance();

            var x = new float[initialPoint.Length];
            Array.Copy(initialPoint, x, initialPoint.Length);

            for (int iter = 0; iter < maxIterations; iter++)
            {
                var grad = gradient(x);
                var nextX = new float[x.Length];

                // x_{n+1} = x_n - α * ∇f(x_n)
                for (int i = 0; i < x.Length; i++)
                    nextX[i] = x[i] - (float)(learningRate * grad[i]);

                // Convergence check
                double change = metric.Distance(x, nextX);
                if (change < tolerance)
                    return nextX;

                x = nextX;
            }

            return x;
        }

        /// <summary>
        /// Gradient descent with momentum (accelerated optimization).
        /// </summary>
        /// <param name="initialPoint">Starting point</param>
        /// <param name="gradient">Gradient function</param>
        /// <param name="learningRate">Step size</param>
        /// <param name="momentum">Momentum coefficient (0-1, typical: 0.9)</param>
        /// <param name="maxIterations">Maximum iterations</param>
        /// <param name="metric">Distance metric for convergence</param>
        /// <param name="tolerance">Convergence tolerance</param>
        /// <returns>Optimized point</returns>
        public static float[] GradientDescentMomentum(
            float[] initialPoint,
            Func<float[], float[]> gradient,
            double learningRate,
            double momentum = 0.9,
            int maxIterations = 1000,
            IDistanceMetric? metric = null,
            double tolerance = 1e-6)
        {
            metric = metric ?? new EuclideanDistance();

            var x = new float[initialPoint.Length];
            var velocity = new float[initialPoint.Length];
            Array.Copy(initialPoint, x, initialPoint.Length);

            for (int iter = 0; iter < maxIterations; iter++)
            {
                var grad = gradient(x);
                var nextX = new float[x.Length];

                // v_{n+1} = β * v_n + α * ∇f(x_n)
                // x_{n+1} = x_n - v_{n+1}
                for (int i = 0; i < x.Length; i++)
                {
                    velocity[i] = (float)(momentum * velocity[i] + learningRate * grad[i]);
                    nextX[i] = x[i] - velocity[i];
                }

                // Convergence check
                double change = metric.Distance(x, nextX);
                if (change < tolerance)
                    return nextX;

                x = nextX;
            }

            return x;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Solve linear system Ax = b using Gaussian elimination.
        /// Simple implementation for small systems (< 100 dimensions).
        /// </summary>
        private static float[]? SolveLinearSystem(float[][] A, float[] b)
        {
            int n = b.Length;
            var matrix = new float[n][];
            for (int i = 0; i < n; i++)
            {
                matrix[i] = new float[n + 1];
                for (int j = 0; j < n; j++)
                    matrix[i][j] = A[i][j];
                matrix[i][n] = -b[i]; // Augmented matrix [A | -b]
            }

            // Forward elimination
            for (int i = 0; i < n; i++)
            {
                // Find pivot
                int maxRow = i;
                for (int k = i + 1; k < n; k++)
                {
                    if (Math.Abs(matrix[k][i]) > Math.Abs(matrix[maxRow][i]))
                        maxRow = k;
                }

                // Swap rows
                var temp = matrix[i];
                matrix[i] = matrix[maxRow];
                matrix[maxRow] = temp;

                // Check for singular matrix
                if (Math.Abs(matrix[i][i]) < 1e-10)
                    return null;

                // Eliminate column
                for (int k = i + 1; k < n; k++)
                {
                    float factor = matrix[k][i] / matrix[i][i];
                    for (int j = i; j <= n; j++)
                        matrix[k][j] -= factor * matrix[i][j];
                }
            }

            // Back substitution
            var x = new float[n];
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = matrix[i][n];
                for (int j = i + 1; j < n; j++)
                    x[i] -= matrix[i][j] * x[j];
                x[i] /= matrix[i][i];
            }

            return x;
        }

        #endregion
    }
}
