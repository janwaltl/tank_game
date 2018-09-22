#version 330 core

uniform vec3 col;

in vec2 fUV;

out vec4 color;


uniform sampler2D tex;

void main()
{
	color =  vec4(col,1.0f) * texture(tex,fUV).xxxx;
}