#version 330

in vec4 fCol;
in vec2 fUV;

out vec4 color;

uniform sampler2D tex;

void main()
{
	color =  fCol * texture(tex,fUV);
}