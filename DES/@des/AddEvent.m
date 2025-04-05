function d=AddEvent(d,e)
 l=length(d.events);
 if (l==0)
    d.events(1)=e;
    return;
 end
 if (get(e,'Time')>get(d.events(l),'Time'))
    d.events(l+1)=e;
    return;
 end
 ev=d.events;
 for i=1:l,
    if (get(e,'Time')<=get(d.events(i),'Time'))
        ev(i)=e;
        ev(i+1:l+1)=d.events(i:l);
        break;
    end
 end
 d.events=ev;