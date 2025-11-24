(function(){
	const q = (s)=>document.querySelector(s);
	const body = document.body;
	function openModal(id){ q('#'+id)?.classList.add('show'); q('#'+id+'-backdrop')?.classList.add('show'); body.style.overflow='hidden'; }
	function closeModal(id){ q('#'+id)?.classList.remove('show'); q('#'+id+'-backdrop')?.classList.remove('show'); body.style.overflow=''; }
	window.bmOpenLogin=()=>openModal('authModal');
	window.bmCloseLogin=()=>closeModal('authModal');
	window.bmSwitchAuth=(tab)=>{
		q('#authTabLogin')?.classList.toggle('active', tab==='login');
		q('#authTabRegister')?.classList.toggle('active', tab==='register');
		q('#authFormLogin')?.classList.toggle('d-none', tab!=='login');
		q('#authFormRegister')?.classList.toggle('d-none', tab!=='register');
	}
	document.addEventListener('click', (e)=>{
		if(e.target?.id==='authModal-backdrop'){ closeModal('authModal'); }
	});
})();

// Lazy-load poster backgrounds for elements with data-bg attribute
(function(){
	const els = document.querySelectorAll('[data-bg]');
	if(!('IntersectionObserver' in window)){
		els.forEach(el=>{ el.style.backgroundImage = `url('${el.getAttribute('data-bg')}')`; });
		return;
	}
	const io = new IntersectionObserver((entries)=>{
		entries.forEach(entry=>{
			if(entry.isIntersecting){
				const el = entry.target; const url = el.getAttribute('data-bg');
				if(url){ el.style.backgroundImage = `url('${url}')`; el.removeAttribute('data-bg'); }
				io.unobserve(el);
			}
		});
	},{ rootMargin: '100px' });
	els.forEach(el=> io.observe(el));
})();

// Theme toggle moved to theme-toggle-enhanced.js
// This function is now handled by the enhanced version with GSAP animations


