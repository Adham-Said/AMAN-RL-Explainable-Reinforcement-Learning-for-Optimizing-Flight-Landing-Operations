function a = testSim(d1, d2, n)

a.server1=0;
a.server2=0;
a.server3=0;
a.server4=0;

a.queue1=[];
a.queue2=[];

a.numguestdelayed = 0;
a.totaldelays = 0;
a.arrival = d1;
a.service = d2;
a.delaylimit = n;
a.t = [];

a.q1 = [];
a.q2 = [];

a.s1 = [];
a.s2 = [];
a.s3 = [];
a.s4 = [];

a.r = randu();

d = des();
a = class(a, 'testSim', d);

disp(d);

%{

clear classes
profile on

tic;
d = testSim(5, 0.1, 1000);
d = runsim(d);
runTime = toc;

p = profile('info');
fprintf('\n Run Time = %s Seconds \n',toc);
disp(p.FunctionTable);
T = struct2table(p.FunctionTable);
sortedT = sortrows(T, 'TotalTime', 'descend');
disp(sortedT(1:10, {'FunctionName', 'TotalTime', 'NumCalls'}));

%}