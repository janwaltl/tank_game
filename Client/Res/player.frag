#version 330

uniform vec3 playerCol;

out vec4 color;
void main()
{
	color = vec4(playerCol,1.0f);
}