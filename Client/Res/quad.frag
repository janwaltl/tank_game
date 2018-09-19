#version 330 core

uniform vec3 col;
out vec4 color;

void main()
{
	color = vec4(col,1.0f);
}