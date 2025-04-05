function d = report(d)

if d.numguestdelayed > 0
    avg = d.totaldelays / d.numguestdelayed;
else
    avg = 0;
end

avgq1 = 0;
avgq2 = 0;

avgs1 = 0;
avgs2 = 0;
avgs3 = 0;
avgs4 = 0;

for i = 2:length(d.t)
    avgq1 = avgq1 + d.q1(i) * (d.t(i) - d.t(i-1));
    avgq2 = avgq2 + d.q2(i) * (d.t(i) - d.t(i-1));

    avgs1 = avgs1 + d.s1(i) * (d.t(i) - d.t(i-1));
    avgs2 = avgs2 + d.s2(i) * (d.t(i) - d.t(i-1));
    avgs3 = avgs3 + d.s3(i) * (d.t(i) - d.t(i-1));
    avgs4 = avgs4 + d.s4(i) * (d.t(i) - d.t(i-1));
end

avgq1 = avgq1 / d.t(end);
avgq2 = avgq2 / d.t(end);

avgs1 = avgs1 / d.t(end);
avgs2 = avgs2 / d.t(end);
avgs3 = avgs3 / d.t(end);
avgs4 = avgs4 / d.t(end);

fprintf('Average Delay = %0.4f\n', avg);

fprintf('Average First Queue Length = %0.4f\n', avgq1);
fprintf('Average Second Queue Length = %0.4f\n', avgq2);

fprintf('Average First Server Utilization = %f %%\n', 100 * avgs1);
fprintf('Average Second Server Utilization = %f %%\n', 100 * avgs2);
fprintf('Average Third Server Utilization = %f %%\n', 100 * avgs3);
fprintf('Average Forth Server Utilization = %f %%\n', 100 * avgs4);

% plot(d.t, d.q);
% figure
% plot(d.t, d.s);

% Open a file in append mode
logFile = fopen('simulation_output.log', 'a');

fprintf(logFile, 'Average Delay = %0.4f\n', avg);

fprintf(logFile, 'Average First Queue Length = %0.4f\n', avgq1);
fprintf(logFile, 'Average Second Queue Length = %0.4f\n', avgq2);

fprintf(logFile, 'Average First Server Utilization = %f %%\n', 100 * avgs1);
fprintf(logFile, 'Average Second Server Utilization = %f %%\n', 100 * avgs2);
fprintf(logFile, 'Average Third Server Utilization = %f %%\n', 100 * avgs3);
fprintf(logFile, 'Average Forth Server Utilization = %f %%\n', 100 * avgs4);
fprintf(logFile, '----\n');

% Close the file
fclose(logFile);

end
