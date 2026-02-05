
CPO/NTU/TIM  labels

Estas labels son tejidas (tela) y se imprimen en 2, 4 o hasta 6 caras, en base a la cantidad de contenido.
El contenido de las distintas caras dependerá de cada caso particular. En general se consideran 3 casos que serán explicados mas adelante.


Tipos de contenido
------------------

Materiales: 
    Se constituye de los titulos (de partes), porcentajes y fibras en todos los idiomas para el caso 1, o bien se dividen los idiomas para los casos 2 y 3

Instrucciones de Lavado:
    Contiene las instrucciones de lavado en todos los idiomas    


Distribucion de datos por caso
------------------------------

Caso 1: Se usa solo si el contenido de materiales (en TODOS los lenguajes) cabe en la cara frontal.

    En este caso se utiliza la cara frontal para colocar el contenido de materiales y el reverso para las instrucciones de lavado.

    Si el contenido de materiales cabe en una sola cara, entonces el codigo del articulo a producir sera seteado al valor:
        - "CPO1"  para archivos .cpo
        - "NTU1"  para archivos .ntu
        - "TIM1"  para archvios .tim


Caso 2: Cuando el contenido de materiales (en TODOS los lenguajes) no pueda ajustarse en una sola cara, se cae en este caso.

    En este caso el contenido de materiales se presentará en dos caras, y el contenido de dichas caras debe dividirse por idiomas:

    Frente:     ingles, espanol, expanol_mexico, catalan, portugues, frances, aleman, holandes, hungaro, italiano, gallego, vasco, rumano, danes e indonesio
    Reverso:    eslovaco, esloveno, checo, leton, lituano, polaco, ruso, griego, bulgaro, turco, chino, japones, coreano, taiwanes y arabe

    Y las instrucciones de lavado se colocarán en una segunda label que será dada de alta como un artefacto de la label principal.

    Si el contenido de materiales cabe en dos caras, entonces el código del articulo a producir sera seteado al valor:
        - "CPO2"  para archivos .cpo
        - "NTU2"  para archivos .ntu
        - "TIM2"  para archvios .tim

    En este caso, las instrucciones de lavado tambien pueden dividirse en dos caras (en la label artefacto), por lo que el sistema deberá medir tambien el contenido
    de las instrucciones de lavado para determinar el texto que aparecerá en la primera cara y el texto que aparecerá en la segunda cara (aunque rara vez la segunda
    cara de esta etiqueta es usada).


Caso 3: Si el contenido de materiales aún despues de haber sido dividido en dos caras no cabe, entonces se cae en este caso.

    En este caso el contenido de materiales se sigue presentando dividido por lenguajes (de la misma forma que en el caso 2), pero se utilizarán 4 caras en vez de solo 2
    para colocar dicho contenido.

    Si el contenido de materiales cabe en 4 caras, entonces el código del articulo a producir sera seteado al valor:
        - "CPO3"  para archivos .cpo
        - "NTU3"  para archivos .ntu
        - "TIM3"  para archvios .tim

    La segunda label de materiales (caras 3 y 4) asi como la label de instrucciones de lavado deberán ser registradas como artefactos.

    Para este caso, el sistema deberá separar el contenido de materiales para que quede distribuido en las 4 caras disponibles de la forma mas efectiva,
    pero siempre tomando en cuenta los lenguajes que van al frente y los lenguajes que van al reverso. Por otro lado, al separar el contenido en caras es
    necesario tomar en cuenta que el split debe efectuarse al inicio de un título, o en otras palabras, no se debe separar un titulo de sus fibras.

    Ejemplo:  Si el contenido del split 1 es:
        % material 1
        % material 2
        Titulo 2
        % material 1
        Titulo 3
        % material 1
        % material 2
        % material 3
        Titulo 4
        % material 1
        % material 2
        
    y digamos que en la cara 1 solo caben las primeras 9 lineas de texto, entonces en la primer cara deberán aparecer las lineas 1 a 8 y el resto del texto en la cara 3.
    Es decir, aunque la linea 9 aún cabe en la cara 1, está línea se envía a la cara 3 porque es un título.


Consideraciones: 

Si en algún momento se detecta que el contenido no puede ser colocado en ninguno de los casos descritos, entonces deberá setearse un flag de error en la data importada;
diseño usará este flag para informar al impresor que el contenido de la label no ha podido ser distribuido correctamente y que debe contactar a IT.



Tabla CPO_TIM_NTU
----------------------
Este catálogo contendrá la data para las etiquetas CPO, TIM, NTU y otras etiquetas de composicion similares.
Sus campos son los siguientes:

code_of_supplier        Extraido del archivo de orden
code_of_order           Extraido del archivo de orden
ref_number_1            Extraido del archivo de orden
ref_number_2            Extraido del archivo de orden

ci_1_symbol             Símbolo de lavado 1: Los siguientes cinco simbolos son básicamente un caracter que al ser impreso en un font específico nos da el simbolo de lavado correspondiente.
ci_2_symbol             Símbolo de lavado 2
ci_3_symbol             Símbolo de lavado 3
ci_4_symbol             Símbolo de lavado 4
ci_5_symbol             Símbolo de lavado 5
ci_split1               Son las instrucciones de lavado concatenadas en todos los idiomas, cada idioma se separa del otro por medio de la siguiente cadena de caracteres: " • " 
ci_split2               Inicialmente estará vacio. El plugin se encargará de determinar si el texto en ci_split1 no cabe en la etiqueta y de ser asi moverá parte del contenido de split1 a split2.
ci_error                Indica si el contenido de las reglas de lavado no cabe en las caras disponibles segun el caso.

materials_all           Son todos los titulos, porcentajes y fibras en todos los idiomas concatenados (con saltos de linea donde corresponda)
materials_split1        Son los titulos, porcentajes y fibras en los idiomas que corresonde a las caras frontales.
materials_split2        Son los titulos, porcentajes y fibras en los idiomas que corresonde a las caras de reverso.
materials_split3        Inicialmente estará vacio. El plugin determinará si parte del contenido de split1 deba pasar a este campo segun el caso.
materials_split4        Inicialmente estará vacio. El plugin determinará si parte del contenido de split2 deba pasar a este campo segun el caso.
materials_error         Indica si el contenido de materiales no cabe en las caras disponibles segun el caso.




Configuración de caras por artículo
-----------------------------------

NOTA: El font para medir si el contenido se ajusta al area disponible siempre será "Arial, 4pt"

    CPO
        Caso 1: Una sola label
            Area frontal: materiales
                Width:      10.5 cm     4.1338"
                Height:     2.9 cm      1.1417"
            Area reverso: lavado
                Width:      9.1 cm      3.5826"
                Height:     2.9 cm      1.1417"

        Caso 2: 2 labels

        1era Label (Materiales)
            Area frontal:
                Width:      10.5 cm     4.1338"
                Height:     2.9 cm      1.1417"
            Area reverso:
                Width:      10.5 cm     4.1338"
                Height:     2.9 cm      1.1417"

        2da Label (Lavado)
            Area frontal:
                Width:      9.1 cm      3.5826"
                Height:     2.9 cm      1.1417"
            Area reverso:
                Width:      10.7 cm     4.2126"
                Height:     2.9 cm      1.1417"

        Caso 3: 3 Labels

        Label principal y 1er artefacto: Materiales
            Area frontal:
                Width:      10.5 cm     4.1338"
                Height:     2.9 cm      1.1417"
            Area reverso:
                Width:      10.5 cm     4.1338"
                Height:     2.9 cm      1.1417"

        2do artefacto: Lavado
            Area frontal:
                Width:      9.1 cm      3.5826"
                Height:     2.9 cm      1.1417"
            Area reverso:
                Width:      10.7 cm     4.2126"
                Height:     2.9 cm      1.1417"




    NTU, TIM:   Son los mismos casos antes descritos, solo cambian las dimensiones de las labels.
        Caso 1:
            Area frente: Materiales
                Width:      76.67 mm        3.0185"  
                Height:     23.26 mm        0.9157"
            Area reverso: Lavado
                Width:      61.61 mm        3.0291"
                Height:     23.88 mm        0.9401"

        Caso 2:

        1era Label: materiales
            Area frente:
                Width:      76.94 mm    3.0291"
                Height:     23.68 mm    0.9322"
            Area reverso:
                Width:      76.39 mm    3.0074"
                Height:     24.36 mm    0.9590"

        2da Label: Lavado
            Area frente:
                Width:      63.44 mm    2.49763"
                Height:     23.57 mm    0.92795"
            Area reverso:
                Width:      78.17 mm    3.07755"
                Height:     22.42 mm    0.88267"

        Caso 3: 

        1era y 2da Labels: Materiales
            Area frente:
                Width:      76.94 mm    3.0291"
                Height:     23.68 mm    0.9322"
            Area reverso:
                Width:      76.39 mm    3.0074"
                Height:     24.36 mm    0.9590"

        3ra Label: Lavado
            Area frente:
                Width:      63.44 mm    2.49763"
                Height:     23.57 mm    0.92795"
            Area reverso:
                Width:      78.17 mm    3.07755"
                Height:     22.42 mm    0.88267"



Datos de entrada y mappings
---------------------------

La data para esta label será colocada en los siguientes campos:
    Materials_ALL       Todos los titulos, porcentajes y fibras en todos los idiomas.
    Materials_Split1    Contenido de materiales para la cara 1 (ingles, espanol, expanol_mexico, catalan, portugues, frances, aleman, holandes, hungaro, italiano, gallego, vasco, rumano, danes e indonesio)
    Materials_Split2    Contenido de materiales para la cara 2 (eslovaco, esloveno, checo, leton, lituano, polaco, ruso, griego, bulgaro, turco, chino, japones, coreano, taiwanes y arabe)
    Materials_Split3    Contenido de materiales para la cara 3 (ingles, espanol, expanol_mexico, catalan, portugues, frances, aleman, holandes, hungaro, italiano, gallego, vasco, rumano, danes e indonesio)
    Materials_Split4    Contenido de materiales para la cara 4 (eslovaco, esloveno, checo, leton, lituano, polaco, ruso, griego, bulgaro, turco, chino, japones, coreano, taiwanes y arabe)
    materials_error     Es un flag que indica que se ha detectado que el contenido no cabe ni siquera en el caso 3. Debe usarse simplemente como una función de control. Los valores posibles son: "0" = Todo en orden, "1" = Contenido no cabe
    CI_Split1           Contenido de instrucciones de lavado para cara 1
    CI_Split2           Contenido de instrucciones de lavado para cara 2 (solo se usa si las inst. de lavado no caben en una sola cara)
    ci_error            Es un flag que indica que se ha detectado que las instrucciones de lavado no caben en las caras disponibles. Los valores posibles son: "0" = Todo en orden, "1" = Contenido no cabe

    (otros campos)      Otros campos requeridos para la label pero que no serán modificados por el plugin

Los campos Materials_SplitN, podrán quedar vacios dependiendo del caso. Por ejemplo para el caso 1, todos estos campos quedarán vacios ya 
que solo se usará Materials_All; para el caso 2, solo los campos Materials_Split1 y Materials_Split2 serán utilizados; finalmente para el caso 3 los
campos Materials_Split1, Materials_Split2, Materials_Split3 y Materials_Split4 serán utilizados.

Adicionalmente si se determina que el contenido de materiales no cabe ni siquiera en el caso 3, entonces el campo Materials_Error será puesto a "1",
en cualquier otro caso el valor de ContentError será "0".

Para las instrucciones de lavado no se requiere dividir el contenido por lenguajes, asi que el sistema simplemente determinará el contenido para
la cara 1 y cara 2. Nota: Usualmente la cara 2 de instrucciones de lavado estará vacia.






