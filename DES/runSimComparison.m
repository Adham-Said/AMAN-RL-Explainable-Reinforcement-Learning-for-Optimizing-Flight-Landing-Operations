%% runSimComparison.m
% This script runs the simulation for multiple queue and server configurations,
% then visualizes the results using 3D surface plots and grouped bar charts.

clear classes
close all
clc

% Define server and queue counts to test
serverCounts = [1, 2];
queueCounts = [1, 2];
numTests = length(serverCounts) * length(queueCounts);

% Preallocate arrays
simTimes = zeros(length(queueCounts), length(serverCounts));
avgServerUtil = zeros(length(queueCounts), length(serverCounts));
avgQueueLength = zeros(length(queueCounts), length(serverCounts));
avgDelay = zeros(length(queueCounts), length(serverCounts));
wallTimes = zeros(length(queueCounts), length(serverCounts));

logFile = 'simulation_output.log';

for q = 1:length(queueCounts)
    for s = 1:length(serverCounts)
        queueNumber = queueCounts(q);
        numServers = serverCounts(s);
        
        d = airportSim(1, 0.5, 1000, queueNumber, numServers);

        % Measure wall-clock time around the simulation run
        tic;
        d = runsim(d, queueNumber, numServers);
        wallTime = toc;

        simTimes(q, s) = get(d, 'CLOCK');
        wallTimes(q, s) = wallTime;

        % Read metrics from log file
        [utilization, queueLength, delay] = extractLastMetrics(logFile);
        avgServerUtil(q, s) = utilization;
        avgQueueLength(q, s) = queueLength;
        avgDelay(q, s) = delay;

        fprintf('Queues: %d, Servers: %d, Simulation Clock Time = %.4f, Wall Clock Time = %.4f sec\n', ...
            queueNumber, numServers, simTimes(q, s), wallTime);
    end
end

%% 3D Surface Plots
[X, Y] = meshgrid(serverCounts, queueCounts);

figure('Name', 'Simulation Clock Time', 'NumberTitle', 'off');
surf(X, Y, simTimes);
xlabel('Number of Servers'); ylabel('Number of Queues'); zlabel('Simulation Clock Time');
title('Simulation Clock Time vs. Queues & Servers'); grid on;

figure('Name', 'Wall Clock Time', 'NumberTitle', 'off');
surf(X, Y, wallTimes);
xlabel('Number of Servers'); ylabel('Number of Queues'); zlabel('Wall Clock Time (sec)');
title('Wall Clock Run Time vs. Queues & Servers'); grid on;

figure('Name', 'Server Utilization', 'NumberTitle', 'off');
surf(X, Y, avgServerUtil);
xlabel('Number of Servers'); ylabel('Number of Queues'); zlabel('Average Server Utilization (%)');
title('Server Utilization vs. Queues & Servers'); grid on;

figure('Name', 'Average Queue Length', 'NumberTitle', 'off');
surf(X, Y, avgQueueLength);
xlabel('Number of Servers'); ylabel('Number of Queues'); zlabel('Average Queue Length');
title('Queue Length vs. Queues & Servers'); grid on;

figure('Name', 'Average Delay', 'NumberTitle', 'off');
surf(X, Y, avgDelay);
xlabel('Number of Servers'); ylabel('Number of Queues'); zlabel('Average Delay');
title('Delay vs. Queues & Servers'); grid on;

%% Grouped Bar Charts
figure('Name', 'Grouped Bar Chart - Time', 'NumberTitle', 'off');
bar3(simTimes);
set(gca, 'XTickLabel', serverCounts, 'YTickLabel', queueCounts);
xlabel('Number of Servers'); ylabel('Number of Queues'); zlabel('Simulation Clock Time');
title('Simulation Time for Different Configurations');

grid on;

figure('Name', 'Grouped Bar Chart - Utilization', 'NumberTitle', 'off');
bar3(avgServerUtil);
set(gca, 'XTickLabel', serverCounts, 'YTickLabel', queueCounts);
xlabel('Number of Servers'); ylabel('Number of Queues'); zlabel('Average Server Utilization (%)');
title('Server Utilization for Different Configurations');
grid on;

figure('Name', 'Grouped Bar Chart - Queue Length', 'NumberTitle', 'off');
bar3(avgQueueLength);
set(gca, 'XTickLabel', serverCounts, 'YTickLabel', queueCounts);
xlabel('Number of Servers'); ylabel('Number of Queues'); zlabel('Average Queue Length');
title('Queue Length for Different Configurations');
grid on;

figure('Name', 'Grouped Bar Chart - Delay', 'NumberTitle', 'off');
bar3(avgDelay);
set(gca, 'XTickLabel', serverCounts, 'YTickLabel', queueCounts);
xlabel('Number of Servers'); ylabel('Number of Queues'); zlabel('Average Delay');
title('Delay for Different Configurations');
grid on;
