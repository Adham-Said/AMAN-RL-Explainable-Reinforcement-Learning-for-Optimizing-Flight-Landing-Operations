function d=handle(d,e)
type=get(e,'Type');
clk=get(d.des,'CLOCK');
switch (type)
    case 'start'
        d.r=nextlcg(d.r,1);
        nextar=clk+nexte(d.r,1,d.arrival);
        d=AddEvent(d,event('arrival',nextar));
    case 'arrival'
        d.r = nextlcg(d.r, 1);
        nextar = clk + nexte(d.r, 1, d.arrival);
        d = AddEvent(d, event('arrival', nextar));

        % Check for available server
        if d.server1 == 0
            d.server1 = 1;
            d.r = nextlcg(d.r, 2);
            nextdep = clk + nexte(d.r, 2, d.service);
            d = AddEvent(d, event('departure1', nextdep, 1)); % Assign to server1
        elseif d.server2 == 0
            d.server2 = 1;
            d.r = nextlcg(d.r, 2);
            nextdep = clk + nexte(d.r, 2, d.service);
            d = AddEvent(d, event('departure2', nextdep, 2)); % Assign to server2
        elseif d.server3 == 0
            d.server3 = 1;
            d.r = nextlcg(d.r, 2);
            nextdep = clk + nexte(d.r, 2, d.service);
            d = AddEvent(d, event('departure3', nextdep, 3)); % Assign to server3
        elseif d.server4 == 0
            d.server4 = 1;
            d.r = nextlcg(d.r, 2);
            nextdep = clk + nexte(d.r, 2, d.service);
            d = AddEvent(d, event('departure4', nextdep, 4)); % Assign to server4
        else
            % All servers are busy, enqueue in appropriate queue
            if length(d.queue1) <= length(d.queue2)
                d.queue1(end + 1) = clk; % Add to queue1
            else
                d.queue2(end + 1) = clk; % Add to queue2
            end
        end
        d.numguestdelayed = d.numguestdelayed + 1;


    case 'departure1'
        if isempty(d.queue1) && isempty(d.queue2)
            d.server1=0;
        else
            if length(d.queue1) >= length(d.queue2)
                d.totaldelays=d.totaldelays+(clk-d.queue1(1));
                d.r=nextlcg(d.r,2);
                nextdep=clk+nexte(d.r,2,d.service);
                d=AddEvent(d,event('departure1',nextdep));
                x=d.queue1(2:length(d.queue1));
                d.queue1=x;
            else
                d.totaldelays=d.totaldelays+(clk-d.queue2(1));
                d.r=nextlcg(d.r,2);
                nextdep=clk+nexte(d.r,2,d.service);
                d=AddEvent(d,event('departure1',nextdep));
                x=d.queue2(2:length(d.queue2));
                d.queue2=x;
            end
        end

    case 'departure2'
        if isempty(d.queue1) && isempty(d.queue2)
            d.server1=0;
        else
            if length(d.queue1) >= length(d.queue2)
                d.totaldelays=d.totaldelays+(clk-d.queue1(1));
                d.r=nextlcg(d.r,2);
                nextdep=clk+nexte(d.r,2,d.service);
                d=AddEvent(d,event('departure2',nextdep));
                x=d.queue1(2:length(d.queue1));
                d.queue1=x;
            else
                d.totaldelays=d.totaldelays+(clk-d.queue2(1));
                d.r=nextlcg(d.r,2);
                nextdep=clk+nexte(d.r,2,d.service);
                d=AddEvent(d,event('departure2',nextdep));
                x=d.queue2(2:length(d.queue2));
                d.queue2=x;
            end
        end

    case 'departure3'
        if isempty(d.queue1) && isempty(d.queue2)
            d.server1=0;
        else
            if length(d.queue1) >= length(d.queue2)
                d.totaldelays=d.totaldelays+(clk-d.queue1(1));
                d.r=nextlcg(d.r,2);
                nextdep=clk+nexte(d.r,2,d.service);
                d=AddEvent(d,event('departure3',nextdep));
                x=d.queue1(2:length(d.queue1));
                d.queue1=x;
            else
                d.totaldelays=d.totaldelays+(clk-d.queue2(1));
                d.r=nextlcg(d.r,2);
                nextdep=clk+nexte(d.r,2,d.service);
                d=AddEvent(d,event('departure3',nextdep));
                x=d.queue2(2:length(d.queue2));
                d.queue2=x;
            end
        end

    case 'departure4'
        if isempty(d.queue1) && isempty(d.queue2)
            d.server1=0;
        else
            if length(d.queue1) >= length(d.queue2)
                d.totaldelays=d.totaldelays+(clk-d.queue1(1));
                d.r=nextlcg(d.r,2);
                nextdep=clk+nexte(d.r,2,d.service);
                d=AddEvent(d,event('departure4',nextdep));
                x=d.queue1(2:length(d.queue1));
                d.queue1=x;
            else
                d.totaldelays=d.totaldelays+(clk-d.queue2(1));
                d.r=nextlcg(d.r,2);
                nextdep=clk+nexte(d.r,2,d.service);
                d=AddEvent(d,event('departure4',nextdep));
                x=d.queue2(2:length(d.queue2));
                d.queue2=x;
            end
        end








    otherwise

end

d.t(length(d.t)+1)=clk;

d.q1(length(d.q1)+1)=length(d.queue1);
d.q2(length(d.q2)+1)=length(d.queue2);

d.s1(length(d.s1)+1)=d.server1;
d.s2(length(d.s2)+1)=d.server2;
d.s3(length(d.s3)+1)=d.server3;
d.s4(length(d.s4)+1)=d.server4;
