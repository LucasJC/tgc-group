// ---------------------------------------------------------
// Ejemplo shader Minimo:
// ---------------------------------------------------------

/**************************************************************************************/
/* Variables comunes */
/**************************************************************************************/

//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matView; //Matriz de View
float4x4 matProjection; //Matriz de projection
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
	Texture = (texDiffuseMap);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

float time = 0;
float luz = 0;
float2 wind;
float meshHeight;
float3 meshPosition;
float bendFactor = 0;

/**************************************************************************************/
/* RenderScene */
/**************************************************************************************/

// This bends the entire plant in the direction of the wind.
// vPos:		The world position of the plant *relative* to the base of the plant.
//			(That means we assume the base is at (0, 0, 0). Ensure this before calling this function).
// vWind:		The current direction and strength of the wind.
// fBendScale:	How much this plant is affected by the wind.
void ApplyMainBending(inout float4 vPos, float2 vWind, float fBendScale)
{
	// Calculate the length from the ground, since we'll need it.
	float fLength = length(vPos);
	// Bend factor - Wind variation is done on the CPU.
	float fBF = vPos.y * fBendScale;
	// Smooth bending factor and increase its nearby height limit.
	fBF += 1.0;
	fBF *= fBF;
	fBF = fBF * fBF - fBF;
	// Displace position
	float4 vNewPos = vPos;
	vNewPos.xz += vWind.xy * fBF;
	// Rescale - this keeps the plant parts from "stretching" by shortening the y (height) while
	// they move about the xz.
	vPos.xyz = normalize(vNewPos.xyz)* fLength;
}

//Input del Vertex Shader
struct VS_INPUT
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT
{
	float4 Position :        POSITION0;
	float2 Texcoord :        TEXCOORD0;
	float4 Color :			COLOR0;
};

//Vertex Shader
VS_OUTPUT vs_main(VS_INPUT Input)
{
	VS_OUTPUT Output;
	
	//Proyectar posicion
	Output.Position = mul(Input.Position, matWorldViewProj);

	//Propago las coordenadas de textura
	Output.Texcoord = Input.Texcoord;

	//Propago el color x vertice
	Output.Color = Input.Color;

	return(Output);
}

//Vertex Shader
VS_OUTPUT vs_bend(VS_INPUT Input)
{
	VS_OUTPUT Output;

	ApplyMainBending(Input.Position, wind, bendFactor);

	//Proyectar posicion
	Output.Position = mul(Input.Position, matWorldViewProj);

	//Propago las coordenadas de textura
	Output.Texcoord = Input.Texcoord;

	//Propago el color x vertice
	Output.Color = Input.Color;

	return(Output);
}

// Ejemplo de un vertex shader que anima la posicion de los vertices
// ------------------------------------------------------------------
VS_OUTPUT vs_main2(VS_INPUT Input)
{
	VS_OUTPUT Output;

	// Animar posicion
	/*Input.Position.x += sin(time)*30*sign(Input.Position.x);
	Input.Position.y += cos(time)*30*sign(Input.Position.y-20);
	Input.Position.z += sin(time)*30*sign(Input.Position.z);
	*/

	// Animar posicion
	float Y = Input.Position.y;
	float Z = Input.Position.z;
	Input.Position.y = Y * cos(time) - Z * sin(time);
	Input.Position.z = Z * cos(time) + Y * sin(time);

	//Proyectar posicion
	Output.Position = mul(Input.Position, matWorldViewProj);

	//Propago las coordenadas de textura
	Output.Texcoord = Input.Texcoord;

	// Animar color
	Input.Color.r = abs(sin(time));
	Input.Color.g = abs(cos(time));

	//Propago el color x vertice
	Output.Color = Input.Color;

	return(Output);
}

//Pixel Shader
float4 ps_main(float2 Texcoord: TEXCOORD0, float4 Color : COLOR0) : COLOR0
{
	// Obtener el texel de textura
	// diffuseMap es el sampler, Texcoord son las coordenadas interpoladas
	float4 fvBaseColor = tex2D(diffuseMap, Texcoord);
	// combino color y textura
	// en este ejemplo combino un 80% el color de la textura y un 20%el del vertice
	float4 result = 0.8*fvBaseColor + 0.2*Color;
	result.a = fvBaseColor.a;
	return result;
}

//Pixel Shader
float4 ps_main2(float2 Texcoord: TEXCOORD0, float4 Color : COLOR0) : COLOR0
{
	// Obtener el texel de textura
	// diffuseMap es el sampler, Texcoord son las coordenadas interpoladas
	float4 fvBaseColor = tex2D(diffuseMap, Texcoord);
	// combino color y textura
	float4 result = fvBaseColor * luz;
	result.a = fvBaseColor.a;
	return result;
}

// ------------------------------------------------------------------
technique RenderScene
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_main2();
	}
}

technique BendScene
{
	pass Pass_0 {
		VertexShader = compile vs_3_0 vs_bend();
		PixelShader = compile ps_3_0 ps_main2();
	}
}