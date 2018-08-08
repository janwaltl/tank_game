#version 330 core
layout(location = 0) in vec3 pos;
layout(location = 1) in vec3 col;

uniform mat4 view;
uniform mat4 proj;

out vec4 fCol;

void main()
{
	fCol=vec4(1.0f,0.0f,0.0f,1.0f);
	gl_Position=proj*view*vec4(pos,1.0f);
}