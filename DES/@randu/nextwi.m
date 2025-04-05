function u=nextwi(r,stream,alpha,beta)
u=r.zrng(stream)/2147483647;
u=-1*alpha.*(log(1-u).^(1/beta));
