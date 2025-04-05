function r=nextlcg(r,stream)
r.zrng(stream)=rem(630360016*r.zrng(stream),2147483647);
