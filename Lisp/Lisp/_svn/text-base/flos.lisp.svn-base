;; TODO Написать инициализацию инстанса класса
;; TODO Генерация классов и слотов должна быть выделена в отдельный dsl
(defpackage :flos)
(in-package :flos)
(export '(
		  configure-meta-info
		  defextender
		  extend
		  extend-class
		  get-class
		  get-extender
		  register-class
		  register-extender
		  with-meta-info
		  *MI*
		  call-next-method
		  self
		  class))

;(set *MI* (MetaInfo.))

(defmacro with-meta-info (name &rest body)
  `(let ((,name (get-service Front.ObjectModel.IMetaInfo.)))
	 ,@body))


(defmacro configure-meta-info (&key (meta-info *MI*) &rest body)
  `(dlet ((*MI* (MetaInfoConfigurator. ,meta-info)))
	  ,@body
	  (.Commit *MI*)))


(defun get-class (class-name)
  (*MI*.GetClass (.Name class-name)))

(defun register-class (class)
  (*MI*.RegisterClass class))

(defun get-extender (ext-name)
  (*MI*.GetExtender (.Name ext-name)))

(defun register-extender (extender)
  (*MI*.RegisterExtender extender))

(defun extend-class (class extender &rest params)
  (let ((class-name (.Name class))
		(extender-name (.Name extender)))
  (*MI*.Extend class-name
			   extender-name
			   (Cons:ToVector params))))

(defun extend (class &rest extensions)
  (let ((cls (if (symbol? class)
				 (get-class class)
			     class)))
	(when cls
	  (.Extend cls (Cons:ToVectorOf ClassDefinition. (map->list (fn (x)
														 (if (symbol? x)
															 (get-class x)
														     x))
													 extensions))))
	cls))

(defmacro defextender (name params &rest body)
  `(register-extender (Extender. (.Name ',name)
								 (Extender:CreateExtenderBody (fn ,(cons 'class params)
																  ,@body)))))


(defpackage :flos-def)
(in-package :flos-def)
(use-package :flos)
(export '(
		  make-instance
		  defclass
		  define-class-slot
		  defmethod
		  ))

(defmacro defclass (name super-classes &optional (slot-definitions nil) &key (extensions nil) (methods nil) (init nil))
  (with-gensyms (cd exts)
	`(let* ((,exts (vector-of Front.ObjectModel.ClassDefinition. ,@(map->list (fn (x) 
																				`(get-class ',x)) 
																			super-classes)))
			(,cd (?? (with-not-null (c (get-class ',name))
					   (c.Extend true ,exts)
					   c)
					 (Front.ObjectModel.ClassDefinition. (.Name ',name)
														,exts
														(vector-of Front.ObjectModel.SlotDefinition.)))))
	   ,@(map->list (fn (x) 
						(cons 'define-class-slot (cons cd
													   (if (list? x) x (list x)))))
					(listize slot-definitions))
	   (register-class ,cd)
	   ,@(map->list (fn (x)
						(cons 'defmethod (cons (first x) (cons name (rest x)))))
					methods)
	   ,@(map->list (fn (x)
						`(extend-class ',name ',(first x) ,@(rest x)))
					(listize extensions))
	   ,cd)))


(defmacro define-class-slot (class slot-name &key (type Object.) (extensions nil) (default-value nil))
  (with-gensyms (sd sname)
	`(let* ((,sname (.Name ',slot-name))
			(,sd (Front.ObjectModel.SlotDefinition. (.Name ,class)
													,sname
													,default-value)))
	   (.AddSlot ,class ,sd)
	   ,@(map->list (fn (x)
						`(extend-class (intern ,(.Name class)) ',(first x) ,sname ,@(rest x)))
						(listize extensions))
	   ,sd)))

(defmacro defmethod (name class params &rest body)
  (with-gensyms (mbody cls mname md)
    `(let* ((,mbody (MethodDefinition:CreateDelegate (fn ,(cons 'self params)
														 (let ((call-next-method (fn (&rest args)
																					 (BehaviorDispatcher:CallNextMethod (Cons:ToVector args)))))
														   ,@body))))
			(,cls (get-class ',class))
			(,mname (.Name ',name))
			(,md (MethodDefinition. (.Name ,cls) ,mname ,mbody)))
	   (.AddMethod ,cls ,md)
	   ,md)))


(defun make-instance (class-name &rest params)
  (let* ((class (if (symbol? class-name)
				   (get-class class-name)
				   class-name))
		 (result (Front.ObjectModel.LObject. class)))
	(when (.CanInvoke result "Initialize")
	  (.Invoke result "Initialize" (Cons:ToVector params))) ; так не падет!
	result))


	
(in-package :user)
(use-package :flos)
