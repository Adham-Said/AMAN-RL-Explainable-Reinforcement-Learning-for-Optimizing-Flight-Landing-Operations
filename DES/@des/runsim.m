function d = runsim(d)

while (~iseos(d))
    if (isempty(d.events))
        break;
    end

    e = d.events(1);
    temp = d.events(2:end);
    d.events = temp;
    c = get(e, 'Time');
    t = get(e, 'Type');

    d.clock = c;
    d = handle(d, e);
end

d = report(d);  % <--- Make sure report returns updated d
end
