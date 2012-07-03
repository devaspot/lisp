;; Front.Lisp BootStrap
(in-package :internal)

(export '(
 *
 *=
 +
 ++
 +=
 -
 --
 -=
 /
 /=
 ??
 ??-str
 any
 assert-eql
 assert-eqv
 assert-not-eql
 bind
 bind*
 bind1
 butlast
 caddr
 cadr
 car
 case
 cdddr
 cddr
 cdr
 comment
 compose
 cond
 concat
 constant?
 create-field-accessor
 create-method-accessor
 create-property-accessor
 declare-field
 declare-method
 declare-property
 defmacro
 defun
 def-binop
 def-method
 def-record
 def-setter
 destructuring-bind
 dlet
 dolist
 dotails
 dotimes
 empty-str?
 eval-app-path
 eval-str
 every
 filter
 find
 for
 for-each
 get-or-add
 keyword
 last
 let
 let*
 letfn
 listize
 load-assembly
 load-assembly-from
 logior
 make-delegate
 make-enum
 make-record
 map
 map-cdr
 map1
 mapcat!
 max
 member
 member-if
 min
 missing?
 and
 nand
 name
 negative?
 next!
 odd?
 parallel-set
 pop!
 positive?
 prn
 prns
 progn
 prs
 push!
 range
 read-str
 reduce
 reverse!
 rotate-set
 set
 setter
 shift-set
 time
 to-bool
 trace
 tree?
 try
 unless
 until
 untrace
 untrace-all
 when
 when-not
 with-dispose
 with-gensyms
 with-not-null
 with-services
 xor
 zero?
 one
 str
 add
 str-delim
 *str-limit
 subtract
 _
 into
 :else
 interpreter-read
 interpreter-eval
 closure-environment
 *application-path*
 __pairize
 list
 get-service
 get-service-provider
 load-service
 load-services
 publish-service
 publish-services
 add-service
 add-services
 initialize-service
 set-event-handler
 set-event-handler-sync
 remove-event-handlers
 raise-event
 log-write
 log-write-line
 log-fail
 log-indent
 log-unindent
 with-log
 with-namespace
 ))
 
 
 
(__set list (fn (&rest args) args))	;hmmm, presumes implementation detail

(__set defun (macro (var params &rest body)
	(cons '__set (list var (cons 'fn (cons params body))))))

(defun progn (&rest fcalls)
	(first (last fcalls)))

(__set error (macro (msg &rest args)
	(list 'throw (list 'LispException. (list 'String:Format msg (cons 'vector args))))))

(__set cond (macro (&rest clauses)
	(if (nil? clauses)
		nil
		(if (eql? (first (first clauses)) :else)
			(second (first clauses))
			(list 'if (first (first clauses))
				(second (first clauses))
				(cons 'cond (rest clauses)))))))
					
(__set and (macro (&rest args)
	(cond	((nil? args) true)
			((nil? (rest args)) (first args))
			(t (list 'if (first args) (cons 'and (rest args)) nil)))))

(defun constant? (exp) 
	(if (cons? exp) 
			(eql? (first exp) 'quote) 
			(not (symbol? exp)))) 

(defun __starts-with? (lst x)
	(and (cons? lst) (eql? (first lst) x)))
	
;from PAIP
(__set backquote (macro (x) (__backquote-expand x)))
	
(defun __backquote-expand (x)
	(cond
		((atom? x) 
		 (if (constant? x) x (list 'quote x)))
		((__starts-with? x 'unquote) 
		 (second x))
		((__starts-with? x 'backquote) 
		 (__backquote-expand (__backquote-expand (second x))))
		((__starts-with? (first x) 'unquote-splicing)
		 (if (nil? (rest x))
			 (second (first x))
			 (list 'append (second (first x)) (__backquote-expand (rest x)))))
		(t (__backquote-combine 
						(__backquote-expand (first x)) 
						(__backquote-expand (rest x)) x))))

(defun __backquote-combine (left right x)
	(cond 
		((and (constant? left) (constant? right))
		 (if (and (eql? (eval left) (first x)) (eql? (eval right) (rest x)))
			 (list 'quote x)
		     (list 'quote (cons (eval left) (eval right)))))
		((nil? right)
		 (list 'list left))
		((__starts-with? right 'list) 
		 (cons 'list (cons left (rest right))))
		(t (list 'cons left right))))

(__set defmacro (macro (name params &rest body)
	(cons '__set (list name (cons 'macro (cons params body))))))

(defmacro car (lst) `(first ,lst))
(defmacro cdr (lst) `(rest ,lst))
(defmacro cadr (lst) `(car (cdr ,lst)))
(defmacro caddr (lst) `(car (cdr (cdr ,lst))))
(defmacro cddr (lst) `(rest (rest ,lst)))
(defmacro cdddr (lst) `(rest (rest (rest ,lst))))


(defmacro nand (&rest args)
	`(not (and ,@args)))
	
(defmacro xor (x y)
	`(if ,x (not ,y) ,y))

(defun odd? (x)
	(not (even? x)))

(defun __pairize (lst)
     (cond 
		((nil? lst)			
		 nil)
		((odd? (len lst))	
		 (error "Expecting even number of arguments"))
		(t				
		 (cons (list (first lst) (second lst))
			   (__pairize (nth-rest 2 lst))))))

(__set let (macro (bindings &rest body)
	`(__let ,(map->list (fn (x) (if (list? x) x (list x 'nil))) bindings)
		 ,@body)))

(defmacro __let (bindings &rest body)
	`((fn ,(map->list first	bindings)
			,@body)
		,@(map->list second	bindings)))

(__set let* (macro (bindings &rest body)
	`(__let* ,(map->list (fn (x) (if (list? x) x (list x 'nil))) bindings)
		 ,@body)))
								
(defmacro __let* (bindings &rest body) 
	(if (nil? bindings) 
		`((fn () ,@body)) 
      `(let (,(first bindings)) (__let* ,(rest bindings) ,@body))))

(defmacro __letr (bindings &rest body)
	`(__let ,(map->list (fn (x) (list (first (first x)) 'nil)) bindings)
		,@(concat! (map->list (fn (x) 
						(list '__set (first (first x)) 
								(list 'fn (rest (first x)) (second x))))
					bindings))
				,@body))

(__set letfn __letr)
(__set flet __letr)

(defmacro dlet (bindings &rest body)
  `(dynamic-let ,(if (nil? (rest bindings)) 
				     (first bindings)
					 (apply append bindings))
	  ,@body))
				 
;set as a macro
(defun def-setter (placefn setfn)
	(placefn.Setter setfn))

(defun setter (placefn)
	(if (not (symbol? placefn))
		placefn
		(let ((setfn placefn.Setter))
			(if setfn
				setfn
				placefn))))

(defmacro when (arg &rest body)
   `(if ,arg (block ,@body)))

(defmacro when-not (arg &rest body)
   `(if (not ,arg)
       (block ,@body)))

(__set unless when-not)

;better, suggested by MH

(defmacro __set1 (place value)
	(if (not (cons? place))
		`(__set ,place ,value)
		(let ((setfn (setter (first place))))
			`(,setfn ,@(rest place) ,value))))

(defun __gen-pairwise-calls (cmd lst)
	(when lst
		(cons (list cmd (first lst) (second lst))
			(__gen-pairwise-calls cmd (nth-rest 2 lst)))))

(defmacro set (&rest args)
	(when args
		`(block ,@(__gen-pairwise-calls '__set1 args))))

;similar to CL mapcan
(defun mapcat! (f &rest lists)
	(apply concat! (apply map->list f lists)))

(defun map-cdr (op lst)
; TODO: when-not может приводить тут к StackOverdlow :-/
  (if (not (nil? lst)) (cons (apply op lst) (map-cdr op (rest lst))) nil))
	
(defun member (obj lst &key (test eql?))
	(cond
		((nil? lst) nil)
		((test obj (first lst)) lst)
		(t (member obj (rest lst) :test test))))

(defun member-if (pred lst)
	(cond
		((nil? lst) nil)
		((pred (first lst)) lst)
		(t (member-if pred (rest lst)))))

(defmacro case (arg &rest clauses)
	(let ((g (gensym)))
		`(let ((,g ,arg))
			(cond ,@(__pairize (mapcat! (fn (cl)
							(let ((key (first cl)))
								`(,(cond 
										((eql? key :else) :else)
										((cons? key) `(member ,g ',key))
										(:else `(eql? ,g ',key)))
									,(second cl))))
							clauses))))))

(defun __destructure (params args bindings)
	(cond 
		((nil? params) bindings)
		((atom? params) (cons `(,params ,args) bindings))
		((cons? params)
			(case (first params)
				((&rest)
					 (cons `(,(second params) ,args) bindings))
				(:else
					(__destructure (first params) `(first ,args)
						(__destructure (rest params) `(rest ,args) bindings)))))))


(defmacro destructuring-bind (params args &rest body)
	(let ((gargs (gensym)))
	  `(let ((,gargs ,args))
		 (__let ,(__destructure params gargs nil) ,@body))))

;now redefine defmacro with destructuring
(defun _make_macro (params body)
	(let ((gargs (gensym)))
		`(macro (&rest ,gargs)
			(destructuring-bind ,params ,gargs ,@body)))) 
		
(defun to-bool (x)
	(if x true false))
	
(defun tree? (lst)
	(to-bool (member-if cons? lst)))
	
(__set defmacro (macro (name spec &rest body)
	(if (member-if (fn (x) (member x '(&optional &key)))	spec)
		`(__set ,name (macro ,spec ,@body))
		`(__set ,name ,(_make_macro spec body)))))


(defmacro until (test &rest body) 
	`(while (not ,test) 
		,@body))

(defmacro for (inits test update &rest body)
	`(let* ,inits
		(while ,test
			(block
				,@body
				,update))))

(defmacro next! (lst)
	`(set ,lst (rest ,lst)))

(defmacro with-gensyms (syms &rest body)
    `(__let ,(map->list (fn (s)
                     `(,s (gensym)))
             syms)
       ,@body))

(defmacro dolist (var-lst &rest body)
	(__let* ((g (gensym))
		  (var (car var-lst))
		  (lst (car (cdr var-lst))))
		`(for ((,g ,lst)) ,g (next! ,g)
			(__let ((,var (first ,g))) 
				,@body))))
				
(defmacro dotails (var-lst &rest body)
	(let ((g (gensym))
		  (var (car var-lst))
		  (lst (car (cdr var-lst))))
		`(for ((,g ,lst)) ,g (next! ,g)
			(let ((,var ,g)) 
				,@body))))
	
(defun keyword (str)
	(intern (+ ":" str)))
	
(defun __params-to-args (params &optional (mode :base))
	(cond 
		((nil? params) nil)
		((eqv? (first params) '&optional) (__params-to-args (rest params) :opt))
		((eqv? (first params) '&key) (__params-to-args (rest params) :key))
		((eqv? (first params) '&rest) nil)
		(t (case mode
				(:base (cons (first params) (__params-to-args (rest params) :base)))
				(:opt  (cons (if (cons? (first params)) 
								(first (first params))
								(first params))
						(__params-to-args (rest params) :opt)))
				(:key (cons (if (cons? (first params)) 
								(first (first params))
								(first params))
						(__params-to-args (rest params) :key)))))))

(defun __rest-param (params)
	(cond 
		((nil? params) nil)
		((eqv? (first params) '&rest) (second params))
		(t (__rest-param (rest params)))))

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; .NET Member Accessors
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defmacro create-method-accessor (type name &rest args)
	`(MethodAccessor. ,type ,name (vector ,@args)))

(defmacro create-property-accessor (type name return-type &rest args)
	`(PropertyAccessor. ,type ,name (vector ,return-type ,@args)))

(defmacro create-field-accessor (type name)
	`(FieldAccessor. ,type ,name))

(defmacro declare-method (name type method-name &rest args)
	`(__set ,name (create-method-accessor ,type ,method-name ,@args)))

(defmacro declare-property (name type property-name return-type &rest args)
	`(__set ,name (create-property-accessor ,type ,property-name ,return-type ,@args)))

(defmacro declare-field (name type field-name)
	`(__set ,name (create-field-accessor ,type ,field-name)))


(declare-method get-hash DictionaryHelper. "GetHash" IDictionary. Object.)
(declare-method set-hash DictionaryHelper. "SetHash" IDictionary. Object. Object.)
(declare-property now DateTime. "Now" DateTime.)

(declare-method enumerator-movenext IEnumerator. "MoveNext")
(declare-property enumerator-current IEnumerator. "Current" Object.)

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

(defmacro def-method (name ((p1 dispatch-type-or-value) &rest params) &rest body)
  (if (not name.IsDefined)
			(set name.GlobalValue (Front.Lisp.SimpleGenericFunction.)))
  `(.AddMethod ,name ,dispatch-type-or-value 
			   (fn ,(cons p1 params) 
				   (let ((call-base-method 
						  (fn () 
							  (apply (.FindBaseMethod ,name ,dispatch-type-or-value) 
									 ,p1 ,@(__params-to-args params) ,(__rest-param params)))))	
										;((.FindBaseMethod ,name ,dispatch-type-or-value) ,p1 ,@params))	
					 ,@body))));)

(defmacro def-binop (name ((p1 dispatch1) (p2 dispatch2)) &rest body)
  (when-not name.IsDefined
			(set nameGlobalValue (BinOp.)))
  `(.AddMethod ,name ,dispatch1 ,dispatch2 
			   (fn ,(list p1 p2) 
				   ,@body)))

(def-method str ((obj true)) 
	"true")

(def-method str ((obj false)) 
	"false")

(def-method str ((obj String.)) 
	(String:Concat "\"" obj "\""))


(set *pr-writer Console:Out)
(set *pr-sep " ")

(defun pr (&rest x)
	(while (and x (rest x))
		(*pr-writer.Write (str (first x)))
		(*pr-writer.Write *pr-sep)
		(next! x))
	(when x
		(*pr-writer.Write (str (first x)))))
	
(defun prn (&rest x)
	(apply pr x)
	(*pr-writer.Write "\r\n"))

(defun prs (&rest x)
	(while (and x (rest x))
		(*pr-writer.Write  (first x))
		(*pr-writer.Write *pr-sep)
		(next! x))
	(when x
		(*pr-writer.Write (first x))))
	
(defun prns (&rest x)
	(apply prs x)
	(*pr-writer.Write "\r\n"))

(defmacro += (x n)
	`(set ,x (add ,x ,n)))

(set one 1)

(defmacro ++ (x)
	`(+= ,x one))

(defmacro __accum (op args)
	(with-gensyms (x result)
		`(let ((,result (first ,args)))
			(dolist (,x (rest ,args))
				(,op ,result ,x))
			,result)))
	
(defun + (&rest args)
	(__accum += args))
	
(defmacro -= (x n)
	`(set ,x (subtract ,x ,n)))

(defmacro -- (x)
	`(-= ,x 1))
	
(defun - (&rest args)
	(__accum -= args))

(defmacro *= (x n)
	`(set ,x (multiply ,x ,n)))

(defun * (&rest args)
	(__accum *= args))

(defmacro /= (x n)
	`(set ,x (divide ,x ,n)))

(defun / (&rest args)
	(__accum /= args))

(def-binop add ((x String.) (y Object.))
	(String:Concat x y.ToString))

(def-binop add ((x Object.) (y String.))
	(String:Concat x.ToString y))

(def-method str ((obj System.Type.))
	(+ obj.Name "."))


(defmacro dotimes ((var n) &rest body)
;  (let ((var (car var-n))
	;	(n (car (cdr var-n))))
	(with-gensyms (gn)
		`(for ((,var 0) (,gn ,n)) (< ,var ,gn) (++ ,var)
			,@body)));)

(defun zero? (x)
	(== x 0))

(defun positive? (x)
	(> x 0))

(defun negative? (x)
	(< x 0))
	
(defun __min (x y)
	(if (< x y) x y))

(defun min (&rest args)
	(let ((result (first args)))
		(for ((args (rest args))) args (next! args)
			(set result (__min result (first args))))
		result))

(defun __max (x y)
	(if (> x  y) x y))

(defun max (&rest args)
	(let ((result (first args)))
		(while (rest args)
			(set args (rest args))
			(set result (__max result (first args))))
		result))

(def-method str-delim ((x Cons.) &key (start true))
	(if start "(" ")"))

(def-method str-delim ((x Array.) &key (start true))
	(if start "[" "]"))

(def-method str-delim ((x IEnumerable.) &key (start true))
	(if start 
		(+ "{" (str (type-of x)) " ")
		"}"))

(def-method str-delim ((x IEnumerator.) &key (start true))
	(if start 
		(+ "{" (str (type-of x)) " ")
		"}"))

(set *str-limit 20)

(defun __strseq (seq)
	(let ((s (StringBuilder. (str-delim seq)))
		  (limit *str-limit))
		(let ((c 0) 
			  (e (get-enum seq)))
			(while (and (< c limit) (enumerator-movenext e))
				(s.Append (str (enumerator-current e)))
				(s.Append " ")
				(++ c)
				(when (and (== c limit) (enumerator-movenext e))
					(s.Append "... ")))
			;trim the trailing space
			(when (> c 0)
				(s.Remove (- s.Length 1) 1)))
		(s.Append (str-delim seq :start false))))

(def-method str ((a Array.))
	(__strseq a))

(def-method str ((a Cons.))
	(__strseq a))
	
(def-method str ((a ICollection.))
	(__strseq a))
		

(def-method str ((obj DictionaryEntry.))
	(+ "{" (str obj.Key) " " (str obj.Value) "}"))

(def-binop subtract ((d1 DateTime.) (d2 DateTime.))
	(d1.Subtract d2))


(defmacro time (&rest body)
    (with-gensyms (start)
      `(let ((,start (now)))
         (block ,@body)
         (- (now) ,start))))

(defun __memberize (sym)
	(intern (+ "." sym)))

(defun __typeize (s)
	(intern (+ s.ToString ".")))

;strip the trailing . and return as string
(defun __untypeize (sym)
	(let ((s sym.ToString))
		(s.Substring 0 (- s.Length 1))))
				
(defun make-record (type &rest args)
	(let ((this (type)))
		(apply init-rec this args)))
		
(defmacro def-record (type &rest fields)
  (let ((typesym (if (cons? type) 
						(first type)
						type)))
	(with-gensyms (this newtype)
		`(let ((,newtype	(Record:CreateRecordType	
									;,(__untypeize typesym)
									,(.ToString typesym)
									,(if (cons? type)
										(second type)
										'Record.))))
			(interpreter.InternType ,newtype)
			;(def-method (init-rec (,this ,typesym) &key ,@fields)
			(def-method init-rec ((,this ,newtype) &key ,@fields)
				(call-base-method)
				,@(map->list (fn (x) (if (cons? x)
									`(,(__memberize (first x)) ,this ,(first x))
									`(,(__memberize x) ,this ,x)))
							fields)
					,this)
			,newtype))))

(set _ Missing:Value)

(defun missing? (x)
	(eql? x _))
	
(def-method str ((m Missing.)) "_")

(defmacro unless (test &rest forms)
  `(if ,test nil (block ,@forms)))

(def-setter 'first 'set-first)
(def-setter 'rest 'set-rest)

(defmacro parallel-set (&rest args)
	(when-not (even? (len args)) 
      (error "odd number of arguments"))
	(let* ((pairs (__pairize args))
		   (syms (map->list (fn (x) (gensym)) 
                 pairs)))
	  `(let ,(__pairize (mapcat! list
					  syms
					  (map->list second pairs)))
			(set ,@(mapcat! list
							(map->list first pairs)
							syms)))))

(defmacro push! (obj place)
    `(set ,place (cons ,obj ,place)))

(defmacro pop! (place)
    `(set ,place (rest ,place)))

(defmacro rotate-set (&rest args)
    `(parallel-set ,@(mapcat! list
               args
               (append (rest args) 
                       (list (first args))))))

(defun reverse! (lst)
     (let (prev)
       (while (cons? lst)
         (parallel-set (rest lst) prev
               prev      lst
               lst       (rest lst)))
		prev))

(defun butlast (lst &optional (n 1))
     (reverse! (nth-rest n (reverse lst))))

(defun last (lst &optional (n 1))
	(let ((l (len lst)))
		(if	(<= l n) 
			lst
			(nth-rest (- l n) lst))))
		
(defmacro shift-set (&rest args)
    (let ((places (butlast args)))
      `(parallel-set ,@(mapcat! list
                 places
                 (append (rest places) 
                         (last args))))))

(defmacro for-each ((var coll) &rest body)
	(let ((enum (gensym)))
		`(let ((,enum (get-enum ,coll)))
			(while (enumerator-movenext ,enum)
				(let ((,var (enumerator-current ,enum)))
					,@body)))))


(defmacro make-enum (inits get &rest move)
	(with-gensyms (movefn getfn)
		`(let* ,inits
			(let ((,getfn (fn () ,get))
				  (,movefn (fn () ,@move)))
				(FnEnumerator. ,getfn ,movefn)))))
		
(defun range (start end &optional (step 1))
	(make-enum
		((x start) (curr start))
		curr
		(set curr x)
		(+= x step)
		(< curr end)))


(defun map1 (key seq)
	(make-enum
		((e (get-enum seq)))
		(key (enumerator-current e))
		(enumerator-movenext e)))

(defun filter (pred seq)
	(make-enum
		((e (get-enum seq)))
		(enumerator-current e)
		(let ((seeking true))
			(while (and seeking (enumerator-movenext e))
				(when (pred (enumerator-current e))
					(set seeking false)))
			(not seeking))))

(defun find (val seq &key (test eqv?))
	(filter (fn (x)
				(test x val))
			seq)) 
			

(defun concat (&rest seqs)
	(make-enum
		((next-e (fn () (get-enum (if (nil? seqs)
									nil 
									(first seqs)))))
		 (e  (next-e)))
		(enumerator-current e)
		(if (enumerator-movenext e)
			true
			(block 
				(while (and (next! seqs) 
							(set e (next-e)) 
							(not (enumerator-movenext e))))
					;no body
				(not (nil? seqs))))))


(def-method into ((coll IList.) seq)
	(for-each (x seq)
		(coll.Add x))
	coll)

(def-method into ((coll Cons.) seq)
	(let ((tail (last coll)))
		(for-each (x seq)
			(block
				(set (rest tail) (Cons. x))
				(set tail (rest tail))))
		coll))

(def-method into ((coll nil) seq)
	(let (tail)
		(for-each (x seq)
			(if (nil? tail)
				(set coll (set tail (Cons. x)))
				(block
					(set (rest tail) (Cons. x))
					(set tail (rest tail)))))
		coll))

(defun reduce (f seq &key init)
	(let* ((e (get-enum seq))
		   (has-items (enumerator-movenext e))
		   (result (cond 
						((not has-items) (if (missing? init) (f) init))
						((missing? init) (enumerator-current e))
						(:else (f init (enumerator-current e))))))
		(when has-items
			(while (enumerator-movenext e)
				(set result (f result (enumerator-current e)))))
		result))

(defun map (f &rest seqs)
	(if (== 1 (len seqs))
		(map1 f (first seqs))
		(make-enum
			((es (into () (map1 get-enum seqs))))
			(apply f (map1 enumerator-current es))
			(let ((ret true))
				(for-each (e es)
					(when-not (enumerator-movenext e)
						(set ret false)))
				ret))))

(defun any (pred &rest seqs)
	(let ((m (apply map pred seqs)) 
		  (found false))
		(while (and (not found) (enumerator-movenext m))
			(set found (enumerator-current m)))
		found))

(defun every (pred &rest seqs)
	(let ((m (apply map pred seqs)) 
		  (found true))
		(while (and found (enumerator-movenext m))
			(set found (enumerator-current m)))
		found))


;todo clarify binding of values vs. expressions
(defun bind1 (func val)
	(fn (&rest args)
		(apply func val args)))

(defmacro bind (func &rest pattern)
	(let* ((args (map->list (fn (x) (if (eql? x '_)
								(gensym)
								x)) 
					pattern))
		   (params (mapcat! (fn (patt arg) 
								(if (eql? patt '_)
									(list arg)
									()))
						pattern args)))
		`(fn ,params (,func ,@args))))
		 
(defmacro bind* (func &rest pattern)
	(let* ((args (append (	map->list (fn (x) 
									(if (eql? x '_)
										(gensym)
										x)) 
								pattern)
							(list (gensym))))
		   (params (append (mapcat! (fn (patt arg) 
								(if (eql? patt '_)
									(list arg)
									()))
								pattern args)
							(cons '&rest (last args)))))
		`(fn ,params (apply ,func ,@args))))


(defun compose (&rest fns)
	(destructuring-bind (f1 &rest flist) ((reverse fns))
		(fn (&rest args)
			(let (result (apply f1 args))
				(dolist (f flist)
					(set result (f result)))
				result))))

;n.b. by default catch binds exception to 'ex
(defmacro try (body &key catch finally (catch-name 'ex))
	`(try-catch-finally
		(fn () ,body)
		,(if (missing? catch)
			nil
			`(fn (,catch-name) ,catch))
		,(if (missing? finally)
			nil 
			`(fn () ,finally))))


(defmacro with-dispose (inits &rest body)
	(let ((disposal (map->list 
						(fn (x) `(when ,(first x) (.IDisposable:Dispose ,(first x))))
					(__pairize inits))))
		`(let* ,inits
			(try 
				(block ,@body)
				:finally
					(block ,@disposal)))))
				
(defmacro trace (&rest fnames)
	(when (cons? fnames)
		(for-each (fname fnames)
			(interpreter.Trace fname)))
	'interpreter.TraceList)

(defmacro untrace (&rest fnames)
	(if (nil? fnames)
		interpreter.UnTraceAll
		(for-each (fname fnames)
			(interpreter.UnTrace fname)))
	'interpreter.TraceList)

(defun untrace-all ()
	(interpreter.UntraceAll))

(defun load-assembly (&rest name) 
	(for-each (f name)
		(interpreter.InternTypesFrom (Assembly:LoadWithPartialName f))))

(defun load-assembly-from (filename)
	(interpreter.InternTypesFrom (Assembly:LoadFrom filename)))
	
(defmacro get-or-add (dictionary key expr)
	(let ((g (gensym)))
		`(let ((,g (,key ,dictionary)))
			(when (nil? ,g)
				(set ,g ,expr)
				(.Add ,dictionary ,key ,g))
			,g)))

;delegators are objects which implement an Invoke matching the signature
;of some delegate type
;they are constructed with an IFunction, and implement Invoke by calling it

;a map of delegate type -> delegator type
(set __delegator-types (Hashtable.))

(set __delegator-assembly 
	(let ((assembly-name (AssemblyName.)))
		(set assembly-name.Name "DelegatorAssembly")
		(AppDomain:CurrentDomain.DefineDynamicAssembly assembly-name 
														AssemblyBuilderAccess:Run)))
														
(set __delegator-module (__delegator-assembly.DefineDynamicModule "DelegatorModule"))

;make a type with a ctor taking an IFunction, 
;with an Invoke function matching the delegate type's, 
;implementing the delegate Invoke on the IFunction's Invoke
(defun __make-delegator-type (type)
	(let* ((invoke-sig (type.GetMethod "Invoke"))
		   (invoke-arg-types (apply vector-of System.Type. 
								(map .ParameterType invoke-sig.GetParameters)))
		   (tb (__delegator-module.DefineType (+ type.Name "Delegator")
					(bit-or TypeAttributes:Class TypeAttributes:Public) Object.))
		   (fn-field (tb.DefineField "fn" IFunction. FieldAttributes:Private))
		   (ctor-arg-types [IFunction.])
		   (cb (tb.DefineConstructor MethodAttributes:Public
								CallingConventions:Standard
								ctor-arg-types))
		   (mb (tb.DefineMethod "Invoke" MethodAttributes:Public 
					invoke-sig.ReturnType
					invoke-arg-types)))
		(let ((cil cb.GetILGenerator))
			(cil.Emit OpCodes:Ldarg_0)
			(cil.Emit OpCodes:Call (.GetConstructor Object. System.Type:EmptyTypes))
			(cil.Emit OpCodes:Ldarg_0)
			(cil.Emit OpCodes:Ldarg_1)
			(cil.Emit OpCodes:Stfld fn-field)
			(cil.Emit OpCodes:Ret))
		(let ((mil mb.GetILGenerator))
			(mil.DeclareLocal (type-of []))
			;create an Object array the size of numargs
			(mil.Emit OpCodes:Ldc_I4 invoke-arg-types.Length)
			(mil.Emit OpCodes:Newarr Object.)
			(mil.Emit OpCodes:Stloc_0)
			;turn the args into objects and place in array
			(for ((i 0)) (< i invoke-arg-types.Length) (++ i)
				(mil.Emit OpCodes:Ldloc_0)
				(mil.Emit OpCodes:Ldc_I4 i)
				(mil.Emit OpCodes:Ldarg (+ i 1))
				(when (.IsValueType (i invoke-arg-types))
					(mil.Emit OpCodes:Box (i invoke-arg-types)))
				(mil.Emit OpCodes:Stelem_Ref))
			;call Invoke on fn member
			(mil.Emit OpCodes:Ldarg_0)
			(mil.Emit OpCodes:Ldfld fn-field)
			(mil.Emit OpCodes:Ldloc_0)
			(mil.Emit OpCodes:Callvirt (.GetMethod IFunction. "Invoke"))
			;above will leave an Object on the stack
			(cond	((eql? invoke-sig.ReturnType Void.)
						(block
							(mil.Emit OpCodes:Pop)
							(mil.Emit OpCodes:Ret)))
					((.IsValueType invoke-sig.ReturnType)
						(block
							(mil.Emit OpCodes:Unbox invoke-sig.ReturnType)
							(mil.Emit OpCodes:Ldobj invoke-sig.ReturnType)
							(mil.Emit OpCodes:Ret)))
					(t
						(block
							(mil.Emit OpCodes:Castclass invoke-sig.ReturnType)
							(mil.Emit OpCodes:Ret))))
		tb.CreateType)))
			
	
;get the delegator type for the delegate type if in cache, else create and cache one
(defun __make-delegator (type f)
	(let ((delegator-type (get-or-add __delegator-types type.FullName
							(__make-delegator-type type))))
		(delegator-type f)))

;make an instance of the delegate bound to the closure	
(defmacro make-delegate (type (&rest args) &rest body)
	`(let ((f (fn ,args ,@body)))
		(Delegate:CreateDelegate 
			,type 
			(__make-delegator ,type f)
			"Invoke")))


(defmacro comment (&rest body) nil)

(defun logior (&rest args)
  (let ((result 0))
    (dolist (i args)
	  (set result (bit-or result i)))))

(declare-method interpreter-read Interpreter. "Read" String. TextReader.)
(declare-method interpreter-eval Interpreter. "Eval" Object. Front.Lisp.IEnvironment.)
(declare-property closure-environment Closure. "Environment" Front.Lisp.IEnvironment.)

(defun read-str (text)
	(interpreter-read interpreter "run-time" (StringReader. text)))

(defmacro eval-str (text)
	`(let ((f (fn () nil)))
	   (interpreter-eval interpreter (read-str ,text) (closure-environment f))))

(defun ?? (p1 p2)
	(if (nil? p1) p2 p1))

(defun empty-str? (text)
	(or (nil? text) (eqv? (.Trim text) "")))

(defun ??-str (s1 s2)
	(if (empty-str? s1) s2 s1))

(defmacro with-not-null ((expr value) &rest body)
	`(let ((,expr ,value))
		(if (not (nil? ,expr)) (progn ,@body))))

(defun listize (lst)
  (map->list (fn (x) (if (list? x) x (list x)))
			 lst))

(defun get-service-provider ()
  (if (.IsDefined (intern "*ServiceProvider*"))
	*ServiceProvider*
	ProviderPublisher:Provider))

(defun get-service (service-type)
  (with-not-null (sp (get-service-provider))
	(.GetService sp service-type)))

(defmacro with-services (services &rest body)
  `(let ,(map->list (fn (x)
						(list (first x) (list 'get-service (second x))))
					services)
	 ,@body))

(defun load-service (service-type service &optional (name nil) (description ""))
  (.LoadService *Loader* 
				service
				service-type
				(?? name (.Name service-type))
				(?? description "")))


(defmacro load-services (&rest services)
  `(progn
	 ,@(map->list (fn (x)
					  (cons 'load-service 
							(cons (first x) 
								  (cons (first (rest x))
										(cons (first (rest (rest x)))
											  (cons (first (rest (rest (rest x)))) nil))))))
				  services)))

(defun add-service (service-type service)
  (.AddService *ServiceProvider* service-type service))

(defmacro add-services (&rest services)
  `(progn
	 ,@(map->list (fn (x)
					  (cons 'add-service 
							(cons (first x) 
								  (cons (second x) nil))))
			  services)))

(defun publish-service (service-type service &optional (name nil) (description "") (proxy ""))
  (.Publish *Loader* 
				service
				service-type
				(?? name (.Name service-type))
				(?? description "")
				(?? proxy "")))

;; Еще и прокси надо передавать!				
(defmacro publish-services (&rest services)
  `(progn
	 ,@(map->list (fn (x)
					  (cons 'publish-service 
							(cons (first x) 
								  (cons (first (rest x))
										(cons (first (rest (rest x)))
											  (cons (first (rest (rest (rest x)))) nil))))))
				  services)))

(defun name (name-str)
  (Name. (vector name-str)))


;; TODO: нужно превратить эту бутафорскую конструкцию
;; во что-то работоспособное! поиск типа в теле Body производится во время парсинга,
;; по этому, все танцы с выставлением NS-ов для поиска идут лесом...
(defmacro with-namespace (ns-list &rest body)
	(let ((res (gensym)) (ns-v (Cons:ToVectorOf String. ns-list))) 
		`(let ((,res nil))
			(dlet ((*Namespace* (Front.Lisp.Namespace:Fork)))
				(.AddNamespace *Namespace* ,ns-v)
				(set ,res (progn ,@body))
				(.Rollback *Namespace*)
				,res))))


(defmacro set-event-handler (event-name event-var &rest body)
	(let ((q (gensym)))
	`(with-services ((,q EventQueue.)) 
		(.RegisterHandler ,q ,event-name 
			(make-delegate EventProcessor. (,event-var) ,body))) ))

(defmacro set-event-handler-sync (event-name  event-var &rest body)
	(let ((q (gensym)))
	`(with-services ((,q EventQueue.)) 
		(.RegisterSyncHandler ,q ,event-name 
			(make-delegate EventProcessor. (,event-var) ,body))) ))
			
(defun remove-event-handlers (name)
	(with-services ((q EventQueue.))
		(q.RemoveHandlers (cast name String.))))
		
(defun raise-event (ev &rest args) 
	(with-services ((q EventQueue.))
		(if (== (type-of ev) String.)				
				(q.Raise (cast String. ev) (Cons:ToVector args))
				(q.Raise (cast Event. ev)))))

;; TODO Это надо будет переписать. нужен нормальный assert с выбираемым test-expression!
(defun assert-eql (s1 s2)
  (unless (eql? s1 s2)
	(throw (ApplicationException. (+ (str s1) " is not equal to " (str s2))))))

(defun assert-eqv (s1 s2)
  (unless (eqv? s1 s2)
	(throw (ApplicationException. (+ (str s1) " is not equal to " (str s2))))))

(defun assert-not-eql (s1 s2)
  (when (eql? s1 s2)
	(throw (ApplicationException. (+ (str s1) " is not equal to " (str s2))))))


(set *application-path* (.BaseDirectory AppDomain:CurrentDomain))

(defun eval-app-path (file-name)
  (System.IO.Path:Combine *application-path* file-name))


(defun log-write (text)
  (Front.Diagnostics.Log:Default.Write text))

(defun log-write-line (text)
  (Front.Diagnostics.Log:Default.WriteLine text))

(defun log-fail (exc)
  (Front.Diagnostics.Log:Default.Fail exc))

(defun log-indent ()
  (Front.Diagnostics.Log:Default.Indent))

(defun log-unindent ()
  (Front.Diagnostics.Log:Default.Unindent))

(defmacro with-log (text &rest body)
  `(progn
	 (log-write ,text)
	 (log-write "...")
	 (progn ,@body)
	 (log-write-line "OK")))

(defpackage :user)
(in-package :user)
(use-package :internal)

(defun initialize-service (service-type)
  (let ((service (get-service service-type)))
	(if service
	  (if (not service.IsInitialized)
		(service.Initialize *ServiceProvider*)))))



