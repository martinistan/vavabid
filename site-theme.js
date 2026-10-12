(() => {
  if (window.__umutTheme) { window.__umutTheme(); return; }
  const excluded = 'img,picture,video,canvas,svg,svg *,style,script,noscript';
  function rgba(value) { const m=value.match(/^rgba?\(([^)]+)\)/);return m?m[1].split(/[,\s/]+/).filter(Boolean).map(Number):null; }
  function pale(c) { return c && (c.length<4||c[3]>.8) && Math.min(...c.slice(0,3))>175 && Math.max(...c.slice(0,3))-Math.min(...c.slice(0,3))<55; }
  function dark(c) { return c&&Math.max(...c.slice(0,3))<145&&(c.length<4||c[3]>.5); }
  function coloredBackdrop(el) {
    for(let p=el;p&&p!==document.documentElement;p=p.parentElement){
      if(p.dataset.umutSurface) return false;
      const c=rgba(getComputedStyle(p).backgroundColor);
      if(c&&(c.length<4||c[3]>.8))return Math.max(...c.slice(0,3))-Math.min(...c.slice(0,3))>60;
    }return false;
  }
  function paint(root) {
    const nodes=[root,...root.querySelectorAll('*')].filter(el=>el instanceof HTMLElement&&!el.matches(excluded));
    const values=nodes.map(el=>({el,cs:getComputedStyle(el)})).map(({el,cs})=>({el,bg:cs.backgroundColor,fg:cs.color,border:cs.borderTopColor,image:cs.backgroundImage}));
    for(const {el,bg,border,image} of values){
      if(pale(rgba(bg))&&!el.hasAttribute('data-umut-surface'))el.dataset.umutSurface=el.matches('html,body,main,header,footer,nav,#__next,#app,#root')?'base':el.matches('input,select,textarea,button,[role=combobox],[role=listbox],[role=option]')?'control':'panel';
      if(pale(rgba(border)))el.dataset.umutBorder='';
      if(/gradient/.test(image)&&/255\s*,\s*255\s*,\s*255/.test(image)&&el.matches('nav *,header *,[class*=swiper],[class*=next],[class*=prev]'))el.dataset.umutFade='';
    }
    for(const {el,fg} of values)if(dark(rgba(fg))&&!coloredBackdrop(el))el.dataset.umutInk='';
    for(const el of nodes.filter(el=>el.matches('nav *,header *,[class*=swiper],[class*=next],[class*=prev]'))){
      for(const side of ['before','after']){const cs=getComputedStyle(el,'::'+side);if(cs.content!=='none'&&(pale(rgba(cs.backgroundColor))||(/gradient/.test(cs.backgroundImage)&&/255\s*,\s*255\s*,\s*255/.test(cs.backgroundImage))))el.setAttribute('data-umut-'+side,'');}
    }
  }
  let queued=false;const pending=new Set();
  function flush(){queued=false;const roots=[...pending];pending.clear();for(const root of roots)if(root.isConnected&&!roots.some(other=>other!==root&&other.contains(root)))paint(root);}
  function enqueue(root){pending.add(root);if(!queued){queued=true;requestAnimationFrame(flush);}}
  window.__umutTheme=()=>enqueue(document.body);window.__umutTheme();
  new MutationObserver(records=>{for(const r of records){if(r.type==='childList')for(const n of r.addedNodes)if(n instanceof HTMLElement)enqueue(n);if(r.type==='attributes'){for(const attr of [...r.target.attributes])if(attr.name.startsWith('data-umut-'))r.target.removeAttribute(attr.name);enqueue(r.target);}}}).observe(document.documentElement,{childList:true,subtree:true,attributes:true,attributeFilter:['class','style','disabled','aria-disabled','aria-selected']});
})();
