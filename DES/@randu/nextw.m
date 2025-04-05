function u=nextw(r,stream,alpha,beta)
u=r.zrng(stream)/2147483647;
u=beta*exp(-log(1-u)/alpha);
